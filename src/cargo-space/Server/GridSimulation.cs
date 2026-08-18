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
        
        // Pawn state
        private Vector2I _pawnPosition;
        private List<Vector2I> _currentPath;
        private PawnState _pawnState;
        private Job _currentJob;
        private int _workTicksRemaining;
        private NetworkBridge _networkBridge;

        public GridSimulation(NetworkBridge networkBridge = null)
        {
            _networkBridge = networkBridge;
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobBoard = new List<Job>();
            _pawnPosition = new Vector2I(0, 0);
            _currentPath = new List<Vector2I>();
            _pawnState = PawnState.Idle;
            _workTicksRemaining = 0;
            
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

        public Vector2I GetPawnPosition()
        {
            return _pawnPosition;
        }

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
            if (_pawnState == PawnState.Idle && _jobBoard.Count > 0)
            {
                _currentJob = _jobBoard[0];
                _jobBoard.RemoveAt(0);
                CalculatePath(_currentJob.Target);
            }
            
            if (_pawnState == PawnState.Walking && _currentPath.Count > 0)
            {
                MoveAlongPath();
            }
            else if (_pawnState == PawnState.Working)
            {
                WorkOnJob();
            }
        }

        private void CalculatePath(Vector2I target)
        {
            var godotPath = _pathfinding.GetIdPath(_pawnPosition, target);
            _currentPath = new List<Vector2I>(godotPath);
            if (_currentPath.Count > 0)
            {
                _pawnState = PawnState.Walking;
                GameLogger.Debug($"Path calculated to {target}, {_currentPath.Count} steps");
            }
        }

        private void MoveAlongPath()
        {
            if (_currentPath.Count > 0)
            {
                Vector2I nextStep = _currentPath[0];
                _currentPath.RemoveAt(0);
                _pawnPosition = nextStep;
                
                GameLogger.Debug($"Pawn moved to {nextStep}");
                
                // If path is complete, transition to Working
                if (_currentPath.Count == 0)
                {
                    _pawnState = PawnState.Working;
                    _workTicksRemaining = 3;
                    GameLogger.Debug("Pawn reached target, starting work");
                }
            }
        }

        private void WorkOnJob()
        {
            _workTicksRemaining--;
            GameLogger.Debug($"Working... {_workTicksRemaining} ticks remaining");
            
            if (_workTicksRemaining <= 0)
            {
                ExecuteJob(_currentJob);
                _networkBridge?.BroadcastJobRemoved(_currentJob.Id);
                _pawnState = PawnState.Idle;
            }
        }

        private void ExecuteJob(Job job)
        {
            if (_grid.ContainsKey(job.Target))
            {
                GridTileData tileData = _grid[job.Target];
                
                if (job.Type == JobType.ToggleState)
                {
                    tileData.State = 1 - tileData.State;
                    _grid[job.Target] = tileData;
                    
                    GameLogger.Debug($"Console at {job.Target} toggled to state {tileData.State}");
                    
                    // Broadcast tile state change to all clients
                    _networkBridge?.BroadcastTileUpdate(job.Target, tileData);
                }
            }
        }

        public void CancelJob(JobId id)
        {
            if (_currentJob.Id == id)
            {
                // Ignore cancellation of an active job for now
                GameLogger.Debug($"CancelJob ignored for active job {id}");
                return;
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
    }
}
