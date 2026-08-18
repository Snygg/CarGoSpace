using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;

namespace CargoSpace.Server
{
    public enum PawnState
    {
        Idle,
        Walking,
        Working
    }

    public class GridSimulation
    {
        private Dictionary<Vector2I, GridTileData> _grid;
        private AStarGrid2D _pathfinding;
        private List<Job> _jobBoard;
        private NetworkBridge _networkBridge;
        
        // Entity-based pawn state
        private Dictionary<PawnId, Pawn> _pawns = new();
        private HashSet<IGridEntity> _dirtyEntities = new();

        public GridSimulation(NetworkBridge networkBridge = null)
        {
            _networkBridge = networkBridge;
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobBoard = new List<Job>();
            
            PawnId startingId = PawnId.Create();
            Pawn startingPawn = new Pawn(startingId, new Vector2I(0, 0));
            _pawns[startingId] = startingPawn;
            _dirtyEntities.Add(startingPawn);
            
            InitializeGrid();
            InitializePathfinding();
        }

        private void InitializeGrid()
        {
            // Create a 5x5 cluster of Deck tiles centered at (0, 0)
            // Surrounded by Space tiles
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    Vector2I coord = new Vector2I(x, y);
                    
                    // 5x5 center area is Deck
                    if (x >= -2 && x <= 2 && y >= -2 && y <= 2)
                    {
                        _grid[coord] = new GridTileData(TileType.Deck);
                    }
                    else
                    {
                        _grid[coord] = new GridTileData(TileType.Space);
                    }
                }
            }
            
            // Place Console tiles on opposite sides of the deck
            _grid[new Vector2I(-2, 0)] = new GridTileData(TileType.Console);
            _grid[new Vector2I(2, 0)] = new GridTileData(TileType.Console);
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
                TileDefinition tileDef = TileRegistry.Get(kvp.Value.Type);
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
            // Validate the target tile exists and is interactable
            if (!_grid.ContainsKey(job.Target))
            {
                GameLogger.Debug($"Job rejected: target {job.Target} not in grid");
                _networkBridge?.SendJobRejected(job.Id, job.OwnerPeerId);
                return;
            }

            TileDefinition tileDef = TileRegistry.Get(_grid[job.Target].Type);
            if (tileDef == null || !tileDef.IsInteractable)
            {
                GameLogger.Debug($"Job rejected: target {job.Target} is not interactable");
                _networkBridge?.SendJobRejected(job.Id, job.OwnerPeerId);
                return;
            }

            _jobBoard.Add(job);
            GameLogger.Debug($"Job added to board: {job.Id} {job.Type} at {job.Target}");

            _networkBridge?.BroadcastJobAdded(job);
        }

        public void Tick()
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.State == PawnState.Idle && _jobBoard.Count > 0)
                {
                    pawn.CurrentJob = _jobBoard[0];
                    _jobBoard.RemoveAt(0);
                    CalculatePath(pawn, pawn.CurrentJob.Target);
                }
                
                if (pawn.State == PawnState.Walking && pawn.CurrentPath.Count > 0)
                {
                    MoveAlongPath(pawn);
                }
                else if (pawn.State == PawnState.Working)
                {
                    WorkOnJob(pawn);
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

        private void WorkOnJob(Pawn pawn)
        {
            pawn.WorkTicksRemaining--;
            GameLogger.Debug($"Pawn {pawn.Id}: working... {pawn.WorkTicksRemaining} ticks remaining");
            
            if (pawn.WorkTicksRemaining <= 0)
            {
                ExecuteJob(pawn);
                _networkBridge?.BroadcastJobRemoved(pawn.CurrentJob.Id);
                pawn.CurrentJob = default;
                pawn.State = PawnState.Idle;
            }
        }

        private void ExecuteJob(Pawn pawn)
        {
            Job job = pawn.CurrentJob;
            if (_grid.ContainsKey(job.Target))
            {
                GridTileData tileData = _grid[job.Target];
                
                if (job.Type == JobType.ToggleState)
                {
                    tileData.State = 1 - tileData.State;
                    _grid[job.Target] = tileData;
                    
                    GameLogger.Debug($"Pawn {pawn.Id}: console at {job.Target} toggled to state {tileData.State}");
                    
                    // Broadcast tile state change to all clients
                    _networkBridge?.BroadcastTileUpdate(job.Target, tileData);
                }
            }
        }

        public void CancelJob(JobId id)
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.CurrentJob.Id == id)
                {
                    pawn.CurrentPath.Clear();
                    pawn.WorkTicksRemaining = 0;
                    pawn.State = PawnState.Idle;
                    pawn.CurrentJob = default;
                    _networkBridge?.BroadcastJobRemoved(id);
                    GameLogger.Debug($"Pawn {pawn.Id}: active job cancelled and interrupted: {id}");
                    return;
                }
            }

            for (int i = 0; i < _jobBoard.Count; i++)
            {
                if (_jobBoard[i].Id == id)
                {
                    _jobBoard.RemoveAt(i);
                    GameLogger.Debug($"Job cancelled and removed: {id}");
                    _networkBridge?.BroadcastJobRemoved(id);
                    return;
                }
            }

            GameLogger.Debug($"CancelJob could not find job {id}");
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
