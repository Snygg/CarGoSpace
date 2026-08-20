using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class GridSimulation : IJobExecutionContext
    {
        private Dictionary<Vector2I, GridTileData> _grid;
        private AStarGrid2D _pathfinding;
        private NetworkBridge _networkBridge;
        private JobManager _jobManager;

        // Entity-based pawn state
        private Dictionary<PawnId, Pawn> _pawns = new();
        private HashSet<IGridEntity> _dirtyEntities = new();

        // Flotsam collection state
        private float _flotsamProgress = 0f;

        private RegionManager _regionManager;
        private LogisticsManager _logisticsManager;
        private PowerManager _powerManager;
        private ConstructionManager _constructionManager;
        private bool _regionsDirty = true;

        public RegionManager RegionManager => _regionManager;

        public GridSimulation(NetworkBridge networkBridge = null, ZoneManager zoneManager = null)
        {
            _networkBridge = networkBridge;
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobManager = new JobManager(networkBridge);
            _pawns = new Dictionary<PawnId, Pawn>();
            _dirtyEntities = new HashSet<IGridEntity>();

            _regionManager = new RegionManager();

            _logisticsManager = new LogisticsManager(this, zoneManager, _jobManager, networkBridge, null);
            _constructionManager = new ConstructionManager(this, _logisticsManager, _jobManager, networkBridge);
            _logisticsManager.ConstructionManager = _constructionManager;
            _powerManager = new PowerManager(this, networkBridge);

            InitializeGrid();
            InitializePathfinding();
        }

        private void InitializeGrid()
        {
            string[] blueprint = new string[]
            {
                ". . C . C . .",
                ". . D . D . .",
                ". . D . D . .",
                ". . D . D . .",
                ". . P . P . .",
                ". H C D C . .",
                ". . . . . . ."
            };

            // Map characters to the StringIds in tiles.json
            Dictionary<char, string> legend = new Dictionary<char, string>
            {
                { '.', "space" },
                { 'D', "deck" },
                { 'C', "console" },
                { 'H', "harpoon" }
            };

            int height = blueprint.Length;
            int width = blueprint[0].Replace(" ", "").Length; // ignore spaces for readability

            int offsetX = width / 2;
            int offsetY = height / 2;

            for (int row = 0; row < height; row++)
            {
                // Remove spaces so ". . C" becomes "..C"
                string cleanRow = blueprint[row].Replace(" ", "");

                for (int col = 0; col < width; col++)
                {
                    char c = cleanRow[col];
                    Vector2I coord = new Vector2I(col - offsetX, row - offsetY);

                    if (c == 'P')
                    {
                        // Spawn a Pawn, and put a Deck tile under them
                        _grid[coord] = new GridTileData(TileRegistry.GetId("deck"));

                        PawnId newPawnId = PawnId.Create();
                        Pawn newPawn = new Pawn(newPawnId, coord);
                        _pawns[newPawnId] = newPawn;
                        _dirtyEntities.Add(newPawn);
                    }
                    else if (c == '.')
                    {
                        _grid[coord] = new GridTileData(TileRegistry.GetId("space"));
                    }
                    else if (c == 'D')
                    {
                        _grid[coord] = new GridTileData(TileRegistry.GetId("deck"));
                    }
                    else if (legend.TryGetValue(c, out string stringId))
                    {
                        byte typeId = TileRegistry.GetId(stringId);
                        // Place surfaces on a deck floor
                        _grid[coord] = new GridTileData(TileRegistry.GetId("deck"), typeId);

                        TileDefinition tileDef = TileRegistry.Get(typeId);
                        if (tileDef != null && (tileDef.HasTag("GeneratesPower") || tileDef.HasTag("ConsumesPower")))
                        {
                            _powerManager.RegisterMachine(coord);
                        }
                    }
                    else
                    {
                        GameLogger.Warning($"InitializeGrid: Unknown blueprint character '{c}' at {coord}. Defaulting to space.");
                        _grid[coord] = new GridTileData(TileRegistry.GetId("space"));
                    }
                }
            }
        }

        private void InitializePathfinding()
        {
            _pathfinding = new AStarGrid2D();
            _pathfinding.Region = new Rect2I(new Vector2I(-3, -3), new Vector2I(7, 7));
            _pathfinding.CellSize = new Vector2I(1, 1);
            _pathfinding.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
            _pathfinding.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan;
            _pathfinding.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
            
            _pathfinding.Update();
            
            // Set walkable/unwalkable tiles
            foreach (var kvp in _grid)
            {
                Vector2I coord = kvp.Key;
                TileDefinition tileDef = kvp.Value.GetEffectiveDefinition();
                bool isWalkable = tileDef != null && tileDef.IsWalkable;
                _pathfinding.SetPointSolid(coord, !isWalkable);
            }
        }

        public Dictionary<Vector2I, GridTileData> GetGrid()
        {
            return new Dictionary<Vector2I, GridTileData>(_grid);
        }

        public IEnumerable<Pawn> GetPawns() => _pawns.Values;

        public bool TryGetTile(Vector2I coord, out GridTileData tileData)
        {
            return _grid.TryGetValue(coord, out tileData);
        }

        public void AddItemToGrid(Vector2I coord, string itemStringId)
        {
            _logisticsManager.AddItemToGrid(coord, itemStringId);
        }

        public void SpawnItemToGrid(Vector2I coord, string itemStringId)
        {
            _logisticsManager.SpawnItemOnGrid(coord, itemStringId);
        }

        public bool RemoveItemFromGrid(Vector2I coord, string itemStringId)
        {
            return _logisticsManager.RemoveItemFromGrid(coord, itemStringId);
        }

        public bool AddJob(Job job)
        {
            // Hazard jobs can target any tile in the grid; SetState must be interactable.
            bool isValidTile = _grid.ContainsKey(job.Target);
            if (job.Type == JobType.SetState)
            {
                isValidTile = IsInteractableTile(job.Target);
            }
            else if (job.Type == JobType.Operate)
            {
                isValidTile = _grid.ContainsKey(job.Target) && _grid[job.Target].GetEffectiveDefinition()?.HasTag("Operable") == true;
            }
            else if (job.Type == JobType.Haul)
            {
                isValidTile = _logisticsManager.IsValidHaulJob(job);
            }
            else if (job.Type == JobType.Supply)
            {
                isValidTile = _constructionManager != null && _constructionManager.IsValidSupplyJob(job.Target, job.Destination, job.ItemId);
            }
            else if (job.Type == JobType.Construct)
            {
                isValidTile = _constructionManager != null && _constructionManager.IsReadyToConstruct(job.Target);
            }
            else if (job.Type == JobType.Deconstruct)
            {
                isValidTile = _grid.ContainsKey(job.Target) && CanDeconstruct(job.Target);
            }

            bool isActivelyWorked = _pawns.Values.Any(p => p.CurrentJob.Target == job.Target);
            return _jobManager.AddJob(job, isValidTile, isActivelyWorked, job.OwnerPeerId);
        }

        private bool CanDeconstruct(Vector2I target)
        {
            if (!_grid.TryGetValue(target, out GridTileData tile))
                return false;

            TileDefinition effective = tile.GetEffectiveDefinition();
            if (effective == null)
                return false;

            // Can remove a surface, or a floor (if there is no surface), but never deconstruct raw space
            if (tile.SurfaceTypeId != 0)
                return true;

            TileDefinition baseDef = TileRegistry.Get(tile.TypeId);
            return baseDef?.Layer != "Base";
        }

        // IJobExecutionContext
        public bool IsInteractableTile(Vector2I target)
        {
            if (!_grid.ContainsKey(target)) return false;
            TileDefinition tileDef = _grid[target].GetEffectiveDefinition();
            return tileDef != null && tileDef.IsInteractable;
        }

        public void SetTileType(Vector2I target, byte typeId)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.TypeId = typeId;
                _grid[target] = tileData;

                TileDefinition tileDef = tileData.GetEffectiveDefinition();
                if (_pathfinding != null && tileDef != null)
                {
                    _pathfinding.SetPointSolid(target, !tileDef.IsWalkable);
                }

                _regionsDirty = true;

                _powerManager?.OnTileChanged(target);
                _networkBridge?.BroadcastTileUpdate(target, tileData);

                if (tileDef != null && !tileDef.IsWalkable)
                {
                    TryRelocatePawnFromUnwalkableTile(target);
                }
            }
        }

        public void SetSurfaceType(Vector2I target, byte typeId)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.SurfaceTypeId = typeId;
                _grid[target] = tileData;

                TileDefinition tileDef = tileData.GetEffectiveDefinition();
                if (_pathfinding != null && tileDef != null)
                {
                    _pathfinding.SetPointSolid(target, !tileDef.IsWalkable);
                }

                _regionsDirty = true;

                _powerManager?.OnTileChanged(target);
                _networkBridge?.BroadcastTileUpdate(target, tileData);

                if (tileDef != null && !tileDef.IsWalkable)
                {
                    TryRelocatePawnFromUnwalkableTile(target);
                }
            }
        }

        public void SetTileState(Vector2I target, int state)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.State = state;
                _grid[target] = tileData;
                _networkBridge?.BroadcastTileUpdate(target, tileData);
            }
        }

        public void SetTileHazard(Vector2I target, byte hazardState)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.HazardState = hazardState;
                _grid[target] = tileData;
                _networkBridge?.BroadcastTileUpdate(target, tileData);
            }
        }

        public void BroadcastTileUpdate(Vector2I target, GridTileData data)
        {
            _networkBridge?.BroadcastTileUpdate(target, data);
        }

        private void TryRelocatePawnFromUnwalkableTile(Vector2I target)
        {
            List<Pawn> pawns = _pawns.Values.Where(p => p.Position == target).ToList();
            if (pawns.Count == 0)
                return;

            HashSet<Vector2I> claimedNeighbors = new();
            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I neighbor = target + dir;
                if (_grid.TryGetValue(neighbor, out GridTileData n) && n.GetEffectiveDefinition()?.IsWalkable == true)
                {
                    claimedNeighbors.Add(neighbor);
                }
            }

            foreach (Pawn pawn in pawns)
            {
                Vector2I? destination = null;
                foreach (Vector2I neighbor in claimedNeighbors)
                {
                    // Prefer an unoccupied neighbor, but use the first walkable one if none are free.
                    if (!_pawns.Values.Any(p => p != pawn && p.Position == neighbor))
                    {
                        destination = neighbor;
                        break;
                    }
                }

                // If every neighbor is already occupied, fall back to the first available walkable tile.
                if (destination == null)
                {
                    destination = claimedNeighbors.FirstOrDefault();
                }

                if (destination.HasValue)
                {
                    pawn.UpdatePosition(destination.Value);
                    _dirtyEntities.Add(pawn);
                    GameLogger.Debug($"Relocated pawn {pawn.Id} from {target} to {destination.Value}");
                }
                else
                {
                    GameLogger.Warning($"No walkable neighbor to relocate pawn {pawn.Id} from {target}");
                }
            }
        }

        public void Tick()
        {
            if (_regionsDirty)
            {
                _regionManager.Recalculate(_grid);
                _jobManager.ClearUnreachableCooldowns();
                _regionsDirty = false;
            }

            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.State == PawnState.Idle && _jobManager.BoardCount > 0)
                {
                    for (int i = 0; i < _jobManager.BoardCount; i++)
                    {
                        Job? potentialJob = _jobManager.ClaimNextAvailableJob(out int index);
                        if (potentialJob == null) break;

                        CalculatePath(pawn, potentialJob.Value.Target);

                        if (pawn.CurrentPath.Count > 0)
                        {
                            // Path found. Assign job.
                            pawn.CurrentJob = potentialJob.Value;
                            GameLogger.Debug($"Pawn {pawn.Id}: claimed job {pawn.CurrentJob.Id} at {pawn.CurrentJob.Target}");
                            break; // Stop looking for jobs
                        }
                        else
                        {
                            // Unreachable. Release reservation and requeue with a short cooldown.
                            GameLogger.Warning($"Job {potentialJob.Value.Id} is unreachable. Requeuing.");
                            _jobManager.Release(potentialJob.Value.Target);
                            _jobManager.RequeueWithCooldown(potentialJob.Value);
                        }
                    }
                }

                if ((pawn.State == PawnState.Walking || pawn.State == PawnState.Carrying) && pawn.CurrentPath.Count > 0)
                {
                    MoveAlongPath(pawn);
                }
                else if (pawn.State == PawnState.Working)
                {
                    WorkOnJob(pawn);
                }
                else if (pawn.State == PawnState.Operating)
                {
                    // Continuous operation: pawn remains locked on the tile.
                }
            }

            float currentRate = CalculateFlotsamRate();
            if (currentRate > 0)
            {
                _flotsamProgress += currentRate;
                if (_flotsamProgress >= 1.0f)
                {
                    _flotsamProgress -= 1.0f;
                    string caughtItem = "scrap_metal";

                    Pawn visualPawn = _pawns.Values.FirstOrDefault(p =>
                        p.State == PawnState.Operating &&
                        _grid.TryGetValue(p.CurrentJob.Target, out GridTileData t) &&
                        t.GetEffectiveDefinition()?.Stats.TryGetValue("HarpoonRate", out float _) == true);

                    if (visualPawn == null) return;

                    Vector2I harpoonTarget = visualPawn.CurrentJob.Target;

                    // Find an adjacent walkable tile
                    Vector2I dropCoord = harpoonTarget;
                    Vector2I[] adjacents = { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down };
                    foreach (var dir in adjacents)
                    {
                        Vector2I test = harpoonTarget + dir;
                        if (_grid.TryGetValue(test, out GridTileData tile) && tile.GetEffectiveDefinition()?.IsWalkable == true)
                        {
                            dropCoord = test;
                            break;
                        }
                    }

                    _logisticsManager.SpawnItemOnGrid(dropCoord, caughtItem);
                    _networkBridge?.BroadcastHarpoonCatch(harpoonTarget, caughtItem);
                }
            }

            _constructionManager.Tick();
            _logisticsManager.Tick();
            _powerManager.Tick();
        }

        // Power and logistics logic moved to dedicated managers.

        private float CalculateFlotsamRate()
        {
            float rate = 0f;
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.State == PawnState.Operating && pawn.CurrentJob.Type == JobType.Operate)
                {
                    if (_grid.TryGetValue(pawn.CurrentJob.Target, out GridTileData tile))
                    {
                        TileDefinition def = tile.GetEffectiveDefinition();
                        if (def != null && def.Stats.TryGetValue("HarpoonRate", out float bonus))
                        {
                            rate += bonus;
                        }
                    }
                }
            }
            return rate;
        }

        private Vector2I? GetWalkableAdjacent(Vector2I target, Vector2I pawnPos)
        {
            Vector2I? best = null;
            float bestDist = float.MaxValue;

            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I neighbor = target + dir;
                if (!_grid.ContainsKey(neighbor) || _pathfinding.IsPointSolid(neighbor))
                    continue;

                float dist = neighbor.DistanceTo(pawnPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = neighbor;
                }
            }

            return best;
        }

        public Vector2I? GetReachableProxy(Vector2I target, Vector2I reference)
        {
            if (_grid.TryGetValue(target, out GridTileData tile) &&
                tile.GetEffectiveDefinition()?.IsWalkable == true)
                return target;

            return GetWalkableAdjacent(target, reference);
        }

        private void CalculatePath(Pawn pawn, Vector2I target)
        {
            // If the target itself is unwalkable (e.g. a Wall or Space-floor blueprint),
            // try to use a neighboring walkable tile and work from there.
            if (_grid.ContainsKey(target) && _pathfinding.IsPointSolid(target))
            {
                Vector2I? adjacent = GetWalkableAdjacent(target, pawn.Position);
                if (!adjacent.HasValue)
                {
                    pawn.CurrentPath.Clear();
                    return;
                }
                target = adjacent.Value;
            }

            var godotPath = _pathfinding.GetIdPath(pawn.Position, target);
            pawn.CurrentPath = new List<Vector2I>(godotPath);
            if (pawn.CurrentPath.Count > 0)
            {
                pawn.State = PawnState.Walking;
                GameLogger.Debug($"Pawn {pawn.Id}: path calculated to {target}, {pawn.CurrentPath.Count} steps");
            }
        }

        public bool IsPathPossible(Vector2I from, Vector2I to)
        {
            if (!_grid.ContainsKey(from) || !_grid.ContainsKey(to))
                return false;

            if (from == to)
                return true;

            var path = _pathfinding.GetIdPath(from, to);
            return path.Count > 0;
        }

        private void MoveAlongPath(Pawn pawn)
        {
            if (pawn.CurrentPath.Count > 0)
            {
                Vector2I nextStep = pawn.CurrentPath[0];
                pawn.CurrentPath.RemoveAt(0);
                pawn.UpdatePosition(nextStep);
                _dirtyEntities.Add(pawn);

                GameLogger.Debug($"Pawn {pawn.Id}: moved to {nextStep}");

                // If path is complete, transition
                if (pawn.CurrentPath.Count == 0)
                {
                    if ((pawn.CurrentJob.Type == JobType.Haul || pawn.CurrentJob.Type == JobType.Supply) && pawn.State != PawnState.Carrying)
                    {
                        // Leg 1 complete: picked up the item, now walk to destination
                        GameLogger.Debug($"Pawn {pawn.Id}: picked up haul at {pawn.Position}, heading to {pawn.CurrentJob.Destination}");

                        // For supply runs, remove the item from the source tile now so the pawn is
                        // conceptually carrying it. If the run aborts before delivery, the item will
                        // be returned to the source tile in ResetPawnState.
                        if (pawn.CurrentJob.Type == JobType.Supply)
                        {
                            _logisticsManager?.RemoveItemFromGrid(pawn.CurrentJob.Target, pawn.CurrentJob.ItemId);
                        }

                        pawn.State = PawnState.Carrying;
                        CalculatePath(pawn, pawn.CurrentJob.Destination);

                        if (pawn.CurrentPath.Count == 0)
                        {
                            GameLogger.Warning($"Pawn {pawn.Id}: no path to haul destination {pawn.CurrentJob.Destination}");

                            if (pawn.CurrentJob.Type == JobType.Supply)
                            {
                                _logisticsManager?.AddItemToGrid(pawn.Position, pawn.CurrentJob.ItemId);
                            }
                            else if (pawn.CurrentJob.Type == JobType.Haul)
                            {
                                _jobManager.RequeueWithCooldown(pawn.CurrentJob);
                            }

                            pawn.State = PawnState.Idle;
                            ResetPawnState(pawn);
                            return;
                        }
                        else
                        {
                            pawn.State = PawnState.Carrying;
                        }
                    }
                    else
                    {
                        pawn.State = PawnState.Working;
                        pawn.WorkTicksRemaining = 3;
                        GameLogger.Debug($"Pawn {pawn.Id}: reached target, starting work");
                    }
                }
            }
        }

        private void ResetPawnState(Pawn pawn)
        {
            if (pawn.CurrentJob.Target != default)
            {
                _jobManager.Release(pawn.CurrentJob.Target);
            }

            if (pawn.CurrentJob.Type == JobType.Haul || pawn.CurrentJob.Type == JobType.Supply)
            {
                _jobManager.Release(pawn.CurrentJob.Destination);
            }

            // PATCH: Notify the ConstructionManager that the supply run failed
            if (pawn.CurrentJob.Type == JobType.Supply)
            {
                _constructionManager?.HandleSupplyJobAborted(pawn.CurrentJob.Destination, pawn.CurrentJob.ItemId);

                // If the pawn had picked up the item, return it to the source tile
                // so it is not lost and another supply run can be attempted.
                if (pawn.State == PawnState.Carrying)
                {
                    _logisticsManager?.AddItemToGrid(pawn.CurrentJob.Target, pawn.CurrentJob.ItemId);
                }
            }

            pawn.CurrentPath.Clear();
            pawn.WorkTicksRemaining = 0;
            pawn.State = PawnState.Idle;
            pawn.CurrentJob = default;
        }

        private void WorkOnJob(Pawn pawn)
        {
            pawn.WorkTicksRemaining--;
            GameLogger.Debug($"Pawn {pawn.Id}: working... {pawn.WorkTicksRemaining} ticks remaining");

            if (pawn.WorkTicksRemaining <= 0)
            {
                _jobManager.ExecuteJob(pawn.CurrentJob, this);

                if (pawn.CurrentJob.Type == JobType.Operate)
                {
                    // Continuous lock-in. Do NOT clear the job or release the reservation.
                    pawn.State = PawnState.Operating;
                    GameLogger.Debug($"Pawn {pawn.Id} locked into Operating state at {pawn.CurrentJob.Target}.");
                }
                else if (pawn.CurrentJob.Type == JobType.Supply)
                {
                    // The pawn is carrying the item and working from an adjacent tile;
                    // just consume it directly for the blueprint at the destination.
                    _constructionManager.ConsumeSupply(pawn.CurrentJob.Destination, pawn.CurrentJob.ItemId);

                    _networkBridge?.BroadcastJobRemoved(pawn.CurrentJob.Id);
                    pawn.CurrentJob = default;
                    pawn.State = PawnState.Idle;
                }
                else if (pawn.CurrentJob.Type == JobType.Construct)
                {
                    _constructionManager.CompleteConstruction(pawn.CurrentJob.Target);

                    _networkBridge?.BroadcastJobRemoved(pawn.CurrentJob.Id);
                    pawn.CurrentJob = default;
                    pawn.State = PawnState.Idle;
                }
                else
                {
                    _networkBridge?.BroadcastJobRemoved(pawn.CurrentJob.Id);
                    pawn.CurrentJob = default;
                    pawn.State = PawnState.Idle;
                }
            }
        }

        private void CancelPawnOperation(Pawn pawn, JobId id, Vector2I target)
        {
            _networkBridge?.BroadcastJobRemoved(id);
            ResetPawnState(pawn);
            SetTileState(target, 0);
            GameLogger.Debug($"Pawn {pawn.Id}: operation cancelled at {target}");
        }

        public void CancelJob(JobId id)
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.CurrentJob.Id == id)
                {
                    if (pawn.CurrentJob.Type == JobType.Operate)
                    {
                        CancelPawnOperation(pawn, id, pawn.CurrentJob.Target);
                        return;
                    }

                    ResetPawnState(pawn);
                    _networkBridge?.BroadcastJobRemoved(id);
                    GameLogger.Debug($"Pawn {pawn.Id}: active job cancelled and interrupted: {id}");
                    return;
                }
            }

            _jobManager.CancelJob(id, _pawns.Values);
        }

        public void CancelOperationAt(Vector2I target)
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.CurrentJob.Target == target && pawn.CurrentJob.Type == JobType.Operate)
                {
                    CancelPawnOperation(pawn, pawn.CurrentJob.Id, target);
                    return;
                }
            }

            GameLogger.Debug($"CancelOperationAt: no operating pawn found at {target}");
        }

        public bool PlaceBlueprint(Vector2I coord, byte targetTypeId)
        {
            return _constructionManager != null && _constructionManager.PlaceBlueprint(coord, targetTypeId);
        }

        public IEnumerable<Blueprint> GetBlueprints()
        {
            return _constructionManager?.GetBlueprints() ?? new List<Blueprint>();
        }

        public bool TryMovePawn(Vector2I target)
        {
            // This method is deprecated - use job system instead
            return false;
        }

        public HashSet<IGridEntity> FlushDirtyEntities()
        {
            var copy = new HashSet<IGridEntity>(_dirtyEntities);
            _dirtyEntities.Clear();
            return copy;
        }

        // Storage and haul logic moved to LogisticsManager.
    }
}
