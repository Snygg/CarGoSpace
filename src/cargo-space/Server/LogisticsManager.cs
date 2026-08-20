using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class LogisticsManager
    {
        private GridSimulation _gridSimulation;
        private ZoneManager _zoneManager;
        private JobManager _jobManager;
        private NetworkBridge _networkBridge;
        private ConstructionManager _constructionManager;

        private Dictionary<Vector2I, List<string>> _groundItems = new();

        public LogisticsManager(GridSimulation gridSimulation, ZoneManager zoneManager, JobManager jobManager, NetworkBridge networkBridge, ConstructionManager constructionManager = null)
        {
            _gridSimulation = gridSimulation;
            _zoneManager = zoneManager;
            _jobManager = jobManager;
            _networkBridge = networkBridge;
            _constructionManager = constructionManager;
        }

        public ConstructionManager ConstructionManager
        {
            get => _constructionManager;
            set => _constructionManager = value;
        }

        public void Tick()
        {
            GenerateHaulJobs();
        }

        public void SpawnItemOnGrid(Vector2I coord, string itemStringId)
        {
            if (!_gridSimulation.TryGetTile(coord, out _))
                return;

            ItemDefinition def = ItemRegistry.Get(itemStringId);
            if (def == null)
            {
                GameLogger.Warning($"SpawnItemOnGrid: unknown item {itemStringId}");
                return;
            }

            Vector2I dropCoord = FindDropTile(coord, itemStringId, def.MaxStack) ?? coord;
            AddItemToGrid(dropCoord, itemStringId);
        }

        public void AddItemToGrid(Vector2I coord, string itemStringId)
        {
            if (!_gridSimulation.TryGetTile(coord, out _))
                return;

            if (!_groundItems.ContainsKey(coord))
            {
                _groundItems[coord] = new List<string>();
            }
            _groundItems[coord].Add(itemStringId);

            GameLogger.Debug($"Added {itemStringId} to {coord}. Tile now has {_groundItems[coord].Count} items.");

            _networkBridge?.BroadcastGroundItemsUpdate(coord, _groundItems[coord]);
        }

        public bool RemoveItemFromGrid(Vector2I coord, string itemStringId)
        {
            if (!_groundItems.TryGetValue(coord, out List<string> items))
            {
                GameLogger.Warning($"RemoveItemFromGrid: no items at {coord}");
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == itemStringId)
                {
                    items.RemoveAt(i);
                    if (items.Count == 0)
                    {
                        _groundItems.Remove(coord);
                    }

                    GameLogger.Debug($"Removed {itemStringId} from {coord}. Tile now has {items.Count} items.");
                    _networkBridge?.BroadcastGroundItemsUpdate(coord, _groundItems.TryGetValue(coord, out List<string> remaining) ? remaining : new List<string>());
                    return true;
                }
            }

            GameLogger.Warning($"RemoveItemFromGrid: {itemStringId} not found at {coord}");
            return false;
        }

        public bool HasGroundItem(Vector2I coord, string itemStringId)
        {
            return _groundItems.TryGetValue(coord, out List<string> items) && items.Contains(itemStringId);
        }

        public bool IsItemStackFull(Vector2I coord, string itemStringId, int maxStack)
        {
            if (!_groundItems.TryGetValue(coord, out List<string> items))
                return false;

            int count = 0;
            foreach (string item in items)
            {
                if (item == itemStringId)
                {
                    count++;
                    if (count >= maxStack)
                        return true;
                }
            }

            return false;
        }

        public bool HasBlueprint(Vector2I coord)
        {
            return _constructionManager != null && _constructionManager.HasBlueprint(coord);
        }

        public Vector2I? FindNearestItem(Vector2I startCoord, string itemStringId)
        {
            Vector2I? origin = _gridSimulation.GetReachableProxy(startCoord, startCoord);
            if (!origin.HasValue)
                return null;

            RegionManager regionManager = _gridSimulation.RegionManager;
            if (!regionManager.TryGetRegion(origin.Value, out int startRegion))
                return null;

            Queue<Vector2I> queue = new();
            HashSet<Vector2I> visited = new();
            queue.Enqueue(origin.Value);
            visited.Add(origin.Value);

            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                if (_groundItems.TryGetValue(current, out List<string> items) && items.Contains(itemStringId))
                {
                    return current;
                }

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I next = current + dir;
                    if (visited.Contains(next))
                        continue;

                    if (!regionManager.TryGetRegion(next, out int nextRegion) || nextRegion != startRegion)
                        continue;

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        public bool IsValidHaulJob(Job job)
        {
            if (!_gridSimulation.TryGetTile(job.Target, out _) || !_gridSimulation.TryGetTile(job.Destination, out _))
                return false;

            if (!HasGroundItem(job.Target, job.ItemId))
                return false;

            if (_zoneManager == null || !_zoneManager.IsStorageZone(job.Destination))
                return false;

            if (!_gridSimulation.TryGetTile(job.Destination, out GridTileData destTile))
                return false;

            TileDefinition destDef = destTile.GetEffectiveDefinition();
            if (destDef == null || !destDef.IsWalkable)
                return false;

            ItemDefinition itemDef = ItemRegistry.Get(job.ItemId);
            int maxStack = itemDef?.MaxStack ?? int.MaxValue;

            return !IsItemStackFull(job.Destination, job.ItemId, maxStack);
        }

        private Vector2I? FindDropTile(Vector2I start, string itemStringId, int maxStack)
        {
            Queue<Vector2I> queue = new();
            HashSet<Vector2I> visited = new();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                if (_gridSimulation.TryGetTile(current, out GridTileData tile))
                {
                    TileDefinition tileDef = tile.GetEffectiveDefinition();
                    if (tileDef != null && tileDef.HasTag("StoresUnidentified") &&
                        !IsItemStackFull(current, itemStringId, maxStack))
                    {
                        return current;
                    }
                }

                foreach (Vector2I dir in new[] { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down })
                {
                    Vector2I next = current + dir;
                    if (!visited.Contains(next) && _gridSimulation.TryGetTile(next, out _))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            GameLogger.Warning($"FindDropTile: no non-full walkable tile found near {start} for {itemStringId}");
            return null;
        }

        private Vector2I? FindStorageDestination(string itemStringId)
        {
            if (_zoneManager == null || _zoneManager.ZoneTiles.Count == 0)
                return null;

            ItemDefinition itemDef = ItemRegistry.Get(itemStringId);
            int maxStack = itemDef?.MaxStack ?? int.MaxValue;

            foreach (Vector2I tile in _zoneManager.ZoneTiles.Keys)
            {
                if (!_gridSimulation.TryGetTile(tile, out GridTileData gridTile))
                    continue;

                TileDefinition tileDef = gridTile.GetEffectiveDefinition();
                if (tileDef == null || !tileDef.IsWalkable)
                    continue;

                if (_jobManager.IsReserved(tile) || _jobManager.HasPendingHaulDestination(tile))
                    continue;

                if (!IsItemStackFull(tile, itemStringId, maxStack))
                {
                    return tile;
                }
            }

            return null;
        }

        private void GenerateHaulJobs()
        {
            if (_zoneManager == null || _zoneManager.ZoneTiles.Count == 0)
                return;

            foreach (var kvp in _groundItems)
            {
                Vector2I itemCoord = kvp.Key;

                if (HasBlueprint(itemCoord))
                    continue;

                if (_zoneManager.IsStorageZone(itemCoord))
                    continue;

                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                // Skip if a pawn is already working this tile or if a job is reserved
                if (_gridSimulation.GetPawns().Any(p => p.CurrentJob.Target == itemCoord))
                    continue;
                if (_jobManager.IsReserved(itemCoord) || _jobManager.HasPendingJobForTarget(itemCoord))
                    continue;

                // Generate one haul job for the first item type on this tile
                string itemId = kvp.Value[0];
                Vector2I? destination = FindStorageDestination(itemId);

                if (destination == null)
                {
                    GameLogger.Debug($"GenerateHaulJobs: no available storage destination for {itemId}");
                    continue;
                }

                // Avoid creating haul jobs that have no reachable path. Resolve walkable
                // proxies for the item and destination, then verify region reachability.
                Vector2I? itemProxy = _gridSimulation.GetReachableProxy(itemCoord, itemCoord);
                Vector2I? destProxy = _gridSimulation.GetReachableProxy(destination.Value, itemCoord);
                if (itemProxy == null || destProxy == null)
                    continue;

                RegionManager regionManager = _gridSimulation.RegionManager;
                bool anyPawnCanReach = _gridSimulation.GetPawns().Any(p => regionManager.IsReachable(p.Position, itemProxy.Value));
                bool itemCanReachDest = regionManager.IsReachable(itemProxy.Value, destProxy.Value);
                if (!anyPawnCanReach || !itemCanReachDest)
                {
                    GameLogger.Debug($"GenerateHaulJobs: no reachable path to haul {itemId} from {itemCoord} to {destination.Value}");
                    continue;
                }

                JobId newJobId = JobId.Create();
                Job haulJob = new Job(newJobId, 0, itemCoord, destination.Value, JobType.Haul, 0, itemId);
                _gridSimulation.AddJob(haulJob);
                GameLogger.Debug($"GenerateHaulJobs: created haul job {newJobId} for {itemId} from {itemCoord} to {destination.Value}");
            }
        }
    }
}
