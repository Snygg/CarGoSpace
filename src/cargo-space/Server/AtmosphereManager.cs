using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public struct RegionAtmosphere
    {
        public byte OxygenLevel;
        public byte Smoke;
        public int EqualizationProgress;
        public int OxygenRegenProgress;
        public bool ExposedToSpace;
    }

    public class AtmosphereManager
    {
        private RegionManager _regionManager;
        private Dictionary<Vector2I, GridTileData> _grid;
        private NetworkBridge _networkBridge;

        private Dictionary<Guid, RegionAtmosphere> _regionData = new();
        private List<LeakEdge> _leakEdges = new();
        private HashSet<Guid> _oxygenatingRegions = new();

        private const int LeakDecayTicks = 3;
        private const int OxygenRegenTicks = 5;
        private const byte LeakHazardState = 2;

        public record LeakEdge(Guid A, Guid B);

        public AtmosphereManager(RegionManager regionManager, Dictionary<Vector2I, GridTileData> grid, NetworkBridge networkBridge)
        {
            _regionManager = regionManager;
            _grid = grid;
            _networkBridge = networkBridge;
        }

        public void HandleRegionDelta(RegionDelta delta)
        {
            if (delta == null)
                return;

            foreach (Guid destroyed in delta.Destroyed)
                _regionData.Remove(destroyed);

            foreach (var (_, consumed) in delta.Merged)
                _regionData.Remove(consumed);

            foreach (RegionEntity created in delta.Created)
            {
                if (!_regionData.ContainsKey(created.Id))
                    _regionData[created.Id] = new RegionAtmosphere { OxygenLevel = 0 };
            }
        }

        public void OnRegionsChanged()
        {
            HandleRegionDelta(_regionManager.LastDelta);

            _oxygenatingRegions.Clear();
            HashSet<LeakEdge> newEdges = new();

            // Ensure every current region has data, defaulting to a vacuum.
            foreach (Guid regionId in _regionManager.GetAllRegions())
            {
                if (!_regionData.ContainsKey(regionId))
                    _regionData[regionId] = new RegionAtmosphere { OxygenLevel = 0 };
            }

            // Scan every tile for oxygen generators and leaky walls.
            foreach (var kvp in _grid)
            {
                Vector2I coord = kvp.Key;
                GridTileData tile = kvp.Value;

                if (IsOxygenGenerator(tile) && TryGetAtmosphereRegion(coord, out Guid oxygenRegion))
                    _oxygenatingRegions.Add(oxygenRegion);

                if (!IsLeakyWall(tile))
                    continue;

                HashSet<Guid> sides = new HashSet<Guid>();
                bool touchesSpace = false;

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I n = coord + dir;

                    if (!_grid.TryGetValue(n, out GridTileData nTile))
                    {
                        touchesSpace = true;
                        continue;
                    }

                    TileDefinition nDef = nTile.GetEffectiveDefinition();
                    if (nDef == null)
                        continue;

                    if (nDef.HasTag("Vacuum"))
                    {
                        touchesSpace = true;
                        continue;
                    }

                    if (nDef.Layer == "Surface" && !nDef.HasTag("OxygenGenerator"))
                        continue;

                    if (TryGetAtmosphereRegion(n, out Guid regionId))
                        sides.Add(regionId);
                }

                if (sides.Count == 0)
                    continue;

                if (touchesSpace)
                {
                    foreach (Guid id in sides)
                        newEdges.Add(NormalizedEdge(id, Guid.Empty));
                }

                var ids = sides.ToList();
                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        newEdges.Add(NormalizedEdge(ids[i], ids[j]));
                    }
                }
            }

            // Any region flagged as touching space is open to the void.
            foreach (Guid regionId in _regionManager.GetAllRegions())
            {
                if (_regionManager.IsRegionExposedToSpace(regionId))
                    newEdges.Add(NormalizedEdge(Guid.Empty, regionId));
            }

            _leakEdges = newEdges.ToList();

            // Determine which regions are directly exposed to space (leak edge to the void).
            HashSet<Guid> exposedRegions = new();
            foreach (LeakEdge edge in _leakEdges)
            {
                if (edge.A == Guid.Empty)
                    exposedRegions.Add(edge.B);
                else if (edge.B == Guid.Empty)
                    exposedRegions.Add(edge.A);
            }

            foreach (Guid regionId in _regionData.Keys.ToList())
            {
                RegionAtmosphere atm = _regionData[regionId];
                atm.ExposedToSpace = exposedRegions.Contains(regionId);
                _regionData[regionId] = atm;
            }

            // Push the current atmosphere for every region so clients don't
            // have to wait for a change to see the initial state.
            foreach (Guid regionId in _regionData.Keys.ToList())
            {
                OnRegionAtmosphereChanged(regionId);
            }
        }

        public void SendAtmosphereToClient(long clientId)
        {
            if (_networkBridge == null)
                return;

            foreach (var kvp in _regionData)
            {
                Guid regionId = kvp.Key;
                if (!_regionManager.TryGetRepresentativeTile(regionId, out Vector2I safeTile))
                    continue;

                RegionAtmosphere atm = kvp.Value;
                _networkBridge.SendRegionAtmosphere(clientId, safeTile, atm.OxygenLevel, atm.Smoke);
            }
        }

        public void Tick()
        {
            Dictionary<Guid, byte> minNeighborOxygen = new();

            foreach (var edge in _leakEdges)
            {
                byte a = GetOxygen(edge.A);
                byte b = GetOxygen(edge.B);

                if (!minNeighborOxygen.ContainsKey(edge.A) || b < minNeighborOxygen[edge.A])
                    minNeighborOxygen[edge.A] = b;
                if (!minNeighborOxygen.ContainsKey(edge.B) || a < minNeighborOxygen[edge.B])
                    minNeighborOxygen[edge.B] = a;
            }

            HashSet<Guid> changed = new();

            foreach (Guid regionId in _regionData.Keys.ToList())
            {
                RegionAtmosphere atm = _regionData[regionId];
                byte minO2 = minNeighborOxygen.GetValueOrDefault(regionId, (byte)2);
                bool hasCore = _oxygenatingRegions.Contains(regionId);

                // A generator can only raise oxygen while the region is airtight.
                // An exposed room decompresses steadily toward vacuum.
                bool losing = atm.OxygenLevel > 0 && (atm.ExposedToSpace || minO2 < atm.OxygenLevel);
                bool gaining = !atm.ExposedToSpace && hasCore && atm.OxygenLevel < 2;

                if (losing)
                    atm.EqualizationProgress++;
                else
                    atm.EqualizationProgress = 0;

                if (gaining)
                    atm.OxygenRegenProgress++;
                else
                    atm.OxygenRegenProgress = 0;

                if (atm.EqualizationProgress >= LeakDecayTicks)
                {
                    atm.OxygenLevel = (byte)Math.Max(atm.OxygenLevel - 1, minO2);
                    atm.EqualizationProgress = 0;
                    atm.OxygenRegenProgress = 0;
                    _regionData[regionId] = atm;
                    changed.Add(regionId);
                }
                else if (atm.OxygenRegenProgress >= OxygenRegenTicks)
                {
                    atm.OxygenLevel++;
                    atm.OxygenRegenProgress = 0;
                    atm.EqualizationProgress = 0;
                    _regionData[regionId] = atm;
                    changed.Add(regionId);
                }
                else
                {
                    _regionData[regionId] = atm;
                }
            }

            foreach (Guid regionId in changed)
                OnRegionAtmosphereChanged(regionId);
        }

        private byte GetOxygen(Guid regionId)
        {
            if (regionId == Guid.Empty)
                return 0;

            if (_regionData.TryGetValue(regionId, out RegionAtmosphere atm))
                return atm.OxygenLevel;

            return 0;
        }

        private void OnRegionAtmosphereChanged(Guid regionId)
        {
            if (!_regionManager.TryGetRepresentativeTile(regionId, out Vector2I safeTile))
                return;

            RegionAtmosphere atm = _regionData[regionId];
            _networkBridge?.BroadcastRegionAtmosphere(safeTile, atm.OxygenLevel, atm.Smoke);
        }

        private bool IsOxygenGenerator(GridTileData tile)
        {
            TileDefinition def = tile.GetEffectiveDefinition();
            return def != null && (def.StringId == "ship_core" || def.HasTag("OxygenGenerator"));
        }

        private bool IsLeakyWall(GridTileData tile)
        {
            TileDefinition def = tile.GetEffectiveDefinition();
            return def != null && def.HasTag("Leaky");
        }

        private LeakEdge NormalizedEdge(Guid a, Guid b)
        {
            return a.CompareTo(b) <= 0 ? new LeakEdge(a, b) : new LeakEdge(b, a);
        }

        private bool IsOpenToSpace(Vector2I coord)
        {
            if (!_grid.TryGetValue(coord, out GridTileData tile))
                return true;

            TileDefinition def = tile.GetEffectiveDefinition();
            if (def == null)
                return true;

            return def.HasTag("Vacuum");
        }

        private bool TryGetAtmosphereRegion(Vector2I coord, out Guid regionId)
        {
            if (_regionManager.TryGetRegion(coord, out regionId))
                return true;

            if (!_grid.TryGetValue(coord, out GridTileData tile))
                return false;

            TileDefinition def = tile.GetEffectiveDefinition();
            if (def == null)
                return false;

            if (def.HasTag("Vacuum"))
                return false;

            if (def.Layer == "Surface" && !def.HasTag("OxygenGenerator"))
                return false;

            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I n = coord + dir;
                if (_regionManager.TryGetRegion(n, out Guid adjacentRegion))
                {
                    regionId = adjacentRegion;
                    return true;
                }
            }

            return false;
        }
    }
}
