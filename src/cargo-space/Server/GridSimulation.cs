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

        public GridSimulation(NetworkBridge networkBridge = null)
        {
            _networkBridge = networkBridge;
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobManager = new JobManager(networkBridge);
            
            PawnId pawn1 = PawnId.Create();
            _pawns[pawn1] = new Pawn(pawn1, new Vector2I(-1, -2));
            _dirtyEntities.Add(_pawns[pawn1]);

            PawnId pawn2 = PawnId.Create();
            _pawns[pawn2] = new Pawn(pawn2, new Vector2I(1, -2));
            _dirtyEntities.Add(_pawns[pawn2]);
            
            InitializeGrid();
            InitializePathfinding();
        }

        private void InitializeGrid()
        {
            // Create a U-shaped ship: left prong (x == -2), right prong (x == 2),
            // and bottom connector (y == -2). Surround with Space tiles.
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    Vector2I coord = new Vector2I(x, y);
                    
                    if (x == -2 || x == 2 || y == -2)
                    {
                        _grid[coord] = new GridTileData(TileRegistry.GetId("deck"));
                    }
                    else
                    {
                        _grid[coord] = new GridTileData(TileRegistry.GetId("space"));
                    }
                }
            }
            
            // Place Console tiles at the extreme ends of the U-shape
            _grid[new Vector2I(-2, 2)] = new GridTileData(TileRegistry.GetId("console"));
            _grid[new Vector2I(2, 2)] = new GridTileData(TileRegistry.GetId("console"));
            _grid[new Vector2I(-2, -2)] = new GridTileData(TileRegistry.GetId("console"));
            _grid[new Vector2I(2, -2)] = new GridTileData(TileRegistry.GetId("console"));

            _grid[new Vector2I(-3, -2)] = new GridTileData(TileRegistry.GetId("harpoon"));
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
                TileDefinition tileDef = TileRegistry.Get(kvp.Value.TypeId);
                bool isWalkable = tileDef != null && tileDef.IsWalkable;
                _pathfinding.SetPointSolid(coord, !isWalkable);
            }
        }

        public Dictionary<Vector2I, GridTileData> GetGrid()
        {
            return new Dictionary<Vector2I, GridTileData>(_grid);
        }

        public IEnumerable<Pawn> GetPawns() => _pawns.Values;

        public void AddJob(Job job)
        {
            // Hazard jobs can target any tile in the grid; SetState must be interactable.
            bool isValidTile = _grid.ContainsKey(job.Target);
            if (job.Type == JobType.SetState)
            {
                isValidTile = IsInteractableTile(job.Target);
            }
            else if (job.Type == JobType.Operate)
            {
                isValidTile = _grid.ContainsKey(job.Target) && TileRegistry.Get(_grid[job.Target].TypeId)?.HasTag("Operable") == true;
            }

            bool isActivelyWorked = _pawns.Values.Any(p => p.CurrentJob.Target == job.Target);
            _jobManager.AddJob(job, isValidTile, isActivelyWorked, job.OwnerPeerId);
        }

        // IJobExecutionContext
        public bool IsInteractableTile(Vector2I target)
        {
            if (!_grid.ContainsKey(target)) return false;
            TileDefinition tileDef = TileRegistry.Get(_grid[target].TypeId);
            return tileDef != null && tileDef.IsInteractable;
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

        public void Tick()
        {
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
                            // Unreachable. Discard job and release reservation.
                            GameLogger.Warning($"Job {potentialJob.Value.Id} is unreachable. Discarding.");
                            _networkBridge?.BroadcastJobRemoved(potentialJob.Value.Id);
                            _jobManager.Release(potentialJob.Value.Target);
                        }
                    }
                }

                if (pawn.State == PawnState.Walking && pawn.CurrentPath.Count > 0)
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
        }

        private void CalculatePath(Pawn pawn, Vector2I target)
        {
            var godotPath = _pathfinding.GetIdPath(pawn.Position, target);
            pawn.CurrentPath = new List<Vector2I>(godotPath);
            if (pawn.CurrentPath.Count > 0)
            {
                pawn.State = PawnState.Walking;
                GameLogger.Debug($"Pawn {pawn.Id}: path calculated to {target}, {pawn.CurrentPath.Count} steps");
            }
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
                
                // If path is complete, transition to Working
                if (pawn.CurrentPath.Count == 0)
                {
                    pawn.State = PawnState.Working;
                    pawn.WorkTicksRemaining = 3;
                    GameLogger.Debug($"Pawn {pawn.Id}: reached target, starting work");
                }
            }
        }

        private void ResetPawnState(Pawn pawn)
        {
            if (pawn.CurrentJob.Target != default)
            {
                _jobManager.Release(pawn.CurrentJob.Target);
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
                    // Continuous Lock-in. Do NOT clear the job or release the reservation.
                    pawn.State = PawnState.Operating;
                    GameLogger.Debug($"Pawn {pawn.Id} locked into Operating state at {pawn.CurrentJob.Target}.");
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
    }
}
