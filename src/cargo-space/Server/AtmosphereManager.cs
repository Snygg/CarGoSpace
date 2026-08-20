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
            _leakEdges.Clear();
            _oxygenatingRegions.Clear();

            // Ensure every current region has data, defaulting to High oxygen.
            foreach (int regionId in _regionManager.GetAllRegions())
            {
                if (!_regionData.ContainsKey(regionId))
                    _regionData[regionId] = new RegionAtmosphere { OxygenLevel = 2 };
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

                    if (nDef.StringId == "wall" || nDef.HasTag("Vacuum"))
                    {
                        if (nDef.HasTag("Vacuum"))
                            touchesSpace = true;
                        continue;
                    }

                    if (TryGetAtmosphereRegion(n, out int regionId))
                        sides.Add(regionId);
                }

                if (sides.Count == 0)
                    continue;

                if (touchesSpace)
                {
                    foreach (int id in sides)
                        _leakEdges.Add(new LeakEdge(id, 0));
                }

                var ids = sides.ToList();
                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        _leakEdges.Add(new LeakEdge(ids[i], ids[j]));
                    }
                }
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

                bool losing = minO2 < atm.OxygenLevel;
                bool gaining = hasCore && atm.OxygenLevel < 2;

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

            return 2;
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
            return def != null && def.StringId == "wall" && tile.HazardState == LeakHazardState;
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

            // Walls and vacuum tiles are not part of any room.
            if (def.StringId == "wall" || def.HasTag("Vacuum"))
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
