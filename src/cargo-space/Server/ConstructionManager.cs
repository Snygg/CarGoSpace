using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class ConstructionManager
    {
        private GridSimulation _gridSimulation;
        private LogisticsManager _logisticsManager;
        private JobManager _jobManager;
        private NetworkBridge _networkBridge;

        private Dictionary<Vector2I, Blueprint> _blueprints = new();
        private Dictionary<(Vector2I Destination, string ItemId), int> _pendingSupplyCounts = new();

        public ConstructionManager(GridSimulation gridSimulation, LogisticsManager logisticsManager, JobManager jobManager, NetworkBridge networkBridge)
        {
            _gridSimulation = gridSimulation;
            _logisticsManager = logisticsManager;
            _jobManager = jobManager;
            _networkBridge = networkBridge;
        }

        public bool HasBlueprint(Vector2I coord) => _blueprints.ContainsKey(coord);

        public IEnumerable<Blueprint> GetBlueprints() => _blueprints.Values;

        public bool IsReadyToConstruct(Vector2I coord)
        {
            return _blueprints.TryGetValue(coord, out Blueprint bp) && IsComplete(bp) && !HasPendingSupplies(coord);
        }

        public bool IsValidSupplyJob(Vector2I sourceCoord, Vector2I blueprintCoord, string itemId)
        {
            if (!_blueprints.TryGetValue(blueprintCoord, out Blueprint bp))
                return false;

            if (!bp.Required.ContainsKey(itemId))
                return false;

            int delivered = bp.Delivered.GetValueOrDefault(itemId, 0);
            int pending = GetPendingSupplyCount(blueprintCoord, itemId);
            if (delivered + pending >= bp.Required[itemId])
                return false;

            return _logisticsManager != null && _logisticsManager.HasGroundItem(sourceCoord, itemId);
        }

        public bool PlaceBlueprint(Vector2I coord, byte targetTypeId)
        {
            if (_gridSimulation == null)
                return false;

            GridTileData currentTile = _gridSimulation.GetTileOrSpace(coord);

            if (_blueprints.ContainsKey(coord))
                return false;

            TileDefinition targetDef = TileRegistry.Get(targetTypeId);
            if (targetDef == null || targetDef.Recipe == null || targetDef.Recipe.Count == 0)
                return false;

            TileDefinition currentDef = currentTile.GetEffectiveDefinition();
            if (currentDef == null)
                return false;

            bool canPlace;

            if (targetDef.Layer == LayerType.Floor)
            {
                // Floors replace the base tile and must not be placed under an existing surface
                if (currentTile.SurfaceTypeId != 0)
                    return false;

                // No reason to build the same floor on top of itself
                if (currentTile.TypeId == targetDef.TypeId)
                    return false;

                if (currentDef.HasTag("Vacuum"))
                {
                    // Reject floating space tiles; must touch existing ship
                    canPlace = IsAdjacentToShip(coord);
                }
                else if (currentDef.Layer == LayerType.Floor)
                {
                    canPlace = true;
                }
                else
                {
                    canPlace = false;
                }
            }
            else if (targetDef.Layer == LayerType.Surface)
            {
                // Surfaces sit on top of a floor. They cannot be placed on another
                // surface, nor can they be placed directly on space/vacuum.
                if (currentTile.SurfaceTypeId != 0)
                {
                    canPlace = false;
                }
                else if (currentDef.HasTag("Vacuum"))
                {
                    canPlace = false;
                }
                else if (currentDef.Layer == LayerType.Floor)
                {
                    canPlace = true;
                }
                else
                {
                    canPlace = false;
                }
            }
            else
            {
                canPlace = false;
            }

            if (!canPlace)
                return false;

            Blueprint bp = new Blueprint
            {
                Position = coord,
                TargetTypeId = targetTypeId,
                Required = new Dictionary<string, int>(targetDef.Recipe)
            };

            _blueprints[coord] = bp;
            GameLogger.Debug($"Blueprint placed at {coord} for {targetDef.Name}");
            _networkBridge?.BroadcastBlueprintState(bp);
            return true;
        }

        private bool IsAdjacentToShip(Vector2I coord)
        {
            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I neighbor = coord + dir;
                if (_gridSimulation.TryGetTile(neighbor, out GridTileData tile))
                {
                    TileDefinition def = tile.GetEffectiveDefinition();
                    if (def != null && !def.HasTag("Vacuum") &&
                        (def.Layer == LayerType.Floor || def.Layer == LayerType.Surface))
                        return true;
                }
            }
            return false;
        }

        public void Tick()
        {
            if (_blueprints.Count == 0) return;

            RegionManager regionManager = _gridSimulation.RegionManager;

            List<Blueprint> snapshots = new List<Blueprint>(_blueprints.Values);
            foreach (Blueprint bp in snapshots)
            {
                if (IsComplete(bp) && !HasPendingSupplies(bp.Position))
                {
                    TrySpawnConstructJob(bp.Position);
                    continue;
                }

                Vector2I? buildProxy = _gridSimulation.GetReachableProxy(bp.Position, bp.Position);
                if (!buildProxy.HasValue)
                    continue;

                foreach (var req in bp.Required)
                {
                    string itemId = req.Key;
                    int required = req.Value;
                    int delivered = bp.Delivered.GetValueOrDefault(itemId, 0);
                    int remaining = required - delivered;

                    if (remaining <= 0)
                        continue;

                    int pending = GetPendingSupplyCount(bp.Position, itemId);
                    int needed = remaining - pending;

                    for (int i = 0; i < needed; i++)
                    {
                        if (_logisticsManager == null)
                            break;

                        Vector2I? source = _logisticsManager.FindNearestItem(bp.Position, itemId);
                        if (!source.HasValue || source.Value == bp.Position)
                            break;

                        if (!regionManager.IsReachable(source.Value, buildProxy.Value))
                        {
                            GameLogger.Debug($"ConstructionManager: supply source {source.Value} not in same region as blueprint at {bp.Position}");
                            break;
                        }

                        bool pawnCanReach = _gridSimulation.GetPawns().Any(p => regionManager.IsReachable(p.Position, source.Value));
                        if (!pawnCanReach)
                        {
                            GameLogger.Debug($"ConstructionManager: no pawn can reach supply source {source.Value}");
                            break;
                        }

                        JobId jobId = JobId.Create();
                        Job supplyJob = new Job(jobId, 0, source.Value, bp.Position, JobType.Supply, 0, itemId);

                        // Validate through GridSimulation so the job board accepts it
                        bool added = _gridSimulation?.AddJob(supplyJob) ?? false;
                        if (added)
                        {
                            IncrementPendingSupply(bp.Position, itemId);
                            GameLogger.Debug($"ConstructionManager: created supply job {jobId} for {itemId} from {source.Value} to {bp.Position}");
                        }
                        else
                        {
                            GameLogger.Debug($"ConstructionManager: supply job {jobId} for {itemId} was rejected");
                        }
                    }
                }
            }
        }

        public void ConsumeSupply(Vector2I coord, string itemId)
        {
            if (!_blueprints.TryGetValue(coord, out Blueprint bp))
            {
                GameLogger.Warning($"ConsumeSupply: no blueprint at {coord}");
                return;
            }

            if (!bp.Required.ContainsKey(itemId))
            {
                GameLogger.Warning($"ConsumeSupply: blueprint at {coord} does not require {itemId}");
                return;
            }

            bp.Delivered[itemId] = bp.Delivered.GetValueOrDefault(itemId, 0) + 1;
            DecrementPendingSupply(coord, itemId);

            GameLogger.Debug($"ConsumeSupply: {itemId} delivered to blueprint at {coord}. {bp.Delivered[itemId]}/{bp.Required[itemId]}");

            _networkBridge?.BroadcastBlueprintState(bp);
        }

        public void CompleteConstruction(Vector2I coord)
        {
            if (!_blueprints.TryGetValue(coord, out Blueprint bp))
            {
                GameLogger.Warning($"CompleteConstruction: no blueprint at {coord}");
                return;
            }

            _blueprints.Remove(coord);

            TileDefinition targetDef = TileRegistry.Get(bp.TargetTypeId);
            if (targetDef != null)
            {
                if (targetDef.Layer == LayerType.Floor)
                {
                    // A new floor clears whatever surface was sitting on the old floor.
                    if (_gridSimulation != null && _gridSimulation.TryGetTile(coord, out GridTileData currentTile) && currentTile.SurfaceTypeId != 0)
                    {
                        _gridSimulation.SetSurfaceType(coord, 0);
                    }

                    _gridSimulation?.SetTileType(coord, bp.TargetTypeId);
                }
                else if (targetDef.Layer == LayerType.Surface)
                {
                    if (_gridSimulation == null || !_gridSimulation.TryGetTile(coord, out GridTileData currentTile))
                    {
                        GameLogger.Warning($"CompleteConstruction: no tile data at {coord}");
                        return;
                    }

                    TileDefinition baseDef = TileRegistry.Get(currentTile.TypeId);
                    if (baseDef == null || baseDef.Layer != LayerType.Floor || baseDef.HasTag("Vacuum"))
                    {
                        GameLogger.Warning($"CompleteConstruction: cannot place surface {targetDef.Name} on non-floor base at {coord}");
                        return;
                    }

                    _gridSimulation?.SetSurfaceType(coord, bp.TargetTypeId);
                }

                _gridSimulation?.SetTileState(coord, 1); // built / active
                GameLogger.Debug($"Construction complete at {coord}: built {targetDef.Name}");
            }
            else
            {
                GameLogger.Warning($"CompleteConstruction: unknown target type {bp.TargetTypeId} at {coord}");
            }

            // Send an empty blueprint state to clear the overlay on clients
            _networkBridge?.BroadcastBlueprintState(new Blueprint { Position = coord });
        }

        private bool IsComplete(Blueprint bp)
        {
            if (bp.Required == null || bp.Required.Count == 0)
                return false;

            foreach (var req in bp.Required)
            {
                int delivered = bp.Delivered.GetValueOrDefault(req.Key, 0);
                if (delivered < req.Value)
                    return false;
            }
            return true;
        }

        private bool HasPendingSupplies(Vector2I destination)
        {
            foreach (var kvp in _pendingSupplyCounts)
            {
                if (kvp.Key.Destination == destination && kvp.Value > 0)
                    return true;
            }
            return false;
        }

        public void HandleSupplyJobAborted(Vector2I destination, string itemId)
        {
            DecrementPendingSupply(destination, itemId);
            GameLogger.Debug($"Supply job aborted for {itemId} at {destination}. Pending count reduced.");
        }

        private void TrySpawnConstructJob(Vector2I coord)
        {
            if (_jobManager == null || _jobManager.HasPendingJobForTarget(coord) || _jobManager.IsReserved(coord))
                return;

            JobId jobId = JobId.Create();
            Job constructJob = new Job(jobId, 0, coord, JobType.Construct, 0);
            _gridSimulation?.AddJob(constructJob);
            GameLogger.Debug($"ConstructionManager: created construct job {jobId} at {coord}");
        }

        private int GetPendingSupplyCount(Vector2I destination, string itemId)
        {
            _pendingSupplyCounts.TryGetValue((destination, itemId), out int count);
            return count;
        }

        private void IncrementPendingSupply(Vector2I destination, string itemId)
        {
            var key = (destination, itemId);
            _pendingSupplyCounts[key] = _pendingSupplyCounts.GetValueOrDefault(key, 0) + 1;
        }

        private void DecrementPendingSupply(Vector2I destination, string itemId)
        {
            var key = (destination, itemId);
            if (_pendingSupplyCounts.TryGetValue(key, out int count))
            {
                if (count <= 1)
                    _pendingSupplyCounts.Remove(key);
                else
                    _pendingSupplyCounts[key] = count - 1;
            }
        }
    }
}
