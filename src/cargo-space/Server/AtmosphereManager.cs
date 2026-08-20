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

        private Dictionary<int, RegionAtmosphere> _regionData = new();
        private List<LeakEdge> _leakEdges = new();
        private HashSet<int> _oxygenatingRegions = new();

        private const int LeakDecayTicks = 3;
        private const int OxygenRegenTicks = 5;
        private const byte LeakHazardState = 2;

        public record LeakEdge(int A, int B);

        public AtmosphereManager(RegionManager regionManager, Dictionary<Vector2I, GridTileData> grid, NetworkBridge networkBridge)
        {
            _regionManager = regionManager;
            _grid = grid;
            _networkBridge = networkBridge;
        }

        public void OnRegionsChanged()
        {
            _oxygenatingRegions.Clear();
            HashSet<LeakEdge> newEdges = new();

            // Ensure every current region has data, defaulting to a vacuum.
            // Rooms will fill from OxygenGenerators over time, making the
            // atmosphere visibly flow in.
            foreach (int regionId in _regionManager.GetAllRegions())
            {
                if (!_regionData.ContainsKey(regionId))
                    _regionData[regionId] = new RegionAtmosphere { OxygenLevel = 0 };
            }

            // Scan every tile for oxygen generators and leaky walls.
            foreach (var kvp in _grid)
            {
                Vector2I coord = kvp.Key;
                GridTileData tile = kvp.Value;

                if (IsOxygenGenerator(tile) && TryGetAtmosphereRegion(coord, out int oxygenRegion))
                    _oxygenatingRegions.Add(oxygenRegion);

                if (!IsLeakyWall(tile))
                    continue;

                HashSet<int> sides = new HashSet<int>();
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

                    if (TryGetAtmosphereRegion(n, out int regionId))
                        sides.Add(regionId);
                }

                if (sides.Count == 0)
                    continue;

                if (touchesSpace)
                {
                    foreach (int id in sides)
                        newEdges.Add(NormalizedEdge(id, 0));
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

            // Any region tile with a missing or Vacuum neighbor is open to space.
            foreach (var kvp in _grid)
            {
                Vector2I coord = kvp.Key;
                if (!_regionManager.TryGetRegion(coord, out int regionId))
                    continue;

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I n = coord + dir;
                    if (IsOpenToSpace(n))
                        newEdges.Add(NormalizedEdge(regionId, 0));
                }
            }

            _leakEdges = newEdges.ToList();

            // Determine which regions are directly exposed to space (leak edge to region 0).
            // A region that is open to vacuum cannot be pressurized by oxygen generators.
            HashSet<int> exposedRegions = new();
            foreach (LeakEdge edge in _leakEdges)
            {
                if (edge.A == 0)
                    exposedRegions.Add(edge.B);
                else if (edge.B == 0)
                    exposedRegions.Add(edge.A);
            }

            foreach (int regionId in _regionData.Keys.ToList())
            {
                RegionAtmosphere atm = _regionData[regionId];
                atm.ExposedToSpace = exposedRegions.Contains(regionId);
                _regionData[regionId] = atm;
            }

            // Push the current atmosphere for every region so clients don't
            // have to wait for a change to see the initial state.
            foreach (int regionId in _regionData.Keys.ToList())
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
                int regionId = kvp.Key;
                if (!_regionManager.TryGetRepresentativeTile(regionId, out Vector2I safeTile))
                    continue;

                RegionAtmosphere atm = kvp.Value;
                _networkBridge.SendRegionAtmosphere(clientId, regionId, safeTile, atm.OxygenLevel, atm.Smoke);
            }
        }

        public void Tick()
        {
            Dictionary<int, byte> minNeighborOxygen = new();

            foreach (var edge in _leakEdges)
            {
                byte a = GetOxygen(edge.A);
                byte b = GetOxygen(edge.B);

                if (!minNeighborOxygen.ContainsKey(edge.A) || b < minNeighborOxygen[edge.A])
                    minNeighborOxygen[edge.A] = b;
                if (!minNeighborOxygen.ContainsKey(edge.B) || a < minNeighborOxygen[edge.B])
                    minNeighborOxygen[edge.B] = a;
            }

            HashSet<int> changed = new();

            foreach (int regionId in _regionData.Keys.ToList())
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

            foreach (int regionId in changed)
                OnRegionAtmosphereChanged(regionId);
        }

        private byte GetOxygen(int regionId)
        {
            if (regionId == 0)
                return 0;

            if (_regionData.TryGetValue(regionId, out RegionAtmosphere atm))
                return atm.OxygenLevel;

            return 0;
        }

        private void OnRegionAtmosphereChanged(int regionId)
        {
            if (!_regionManager.TryGetRepresentativeTile(regionId, out Vector2I safeTile))
                return;

            RegionAtmosphere atm = _regionData[regionId];
            _networkBridge?.BroadcastRegionAtmosphere(regionId, safeTile, atm.OxygenLevel, atm.Smoke);
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

        private LeakEdge NormalizedEdge(int a, int b)
        {
            return new LeakEdge(Math.Min(a, b), Math.Max(a, b));
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

        private bool TryGetAtmosphereRegion(Vector2I coord, out int regionId)
        {
            if (_regionManager.TryGetRegion(coord, out regionId))
                return true;

            if (!_grid.TryGetValue(coord, out GridTileData tile))
                return false;

            TileDefinition def = tile.GetEffectiveDefinition();
            if (def == null)
                return false;

            // Vacuum tiles, leaky walls, and non-generator surfaces are not part of any room.
            if (def.HasTag("Vacuum"))
                return false;

            if (def.Layer == "Surface" && !def.HasTag("OxygenGenerator"))
                return false;

            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I n = coord + dir;
                if (_regionManager.TryGetRegion(n, out int adjacentRegion))
                {
                    regionId = adjacentRegion;
                    return true;
                }
            }

            return false;
        }
    }
}
