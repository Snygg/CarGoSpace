using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Server
{
    public class GridSimulation
    {
        private Dictionary<Vector2I, GridTileData> _grid;
        private AStarGrid2D _pathfinding;
        private Queue<Vector2I> _jobBoard;
        
        // Pawn state
        private Vector2I _pawnPosition;
        private List<Vector2I> _currentPath;
        private bool _isBusy;

        public GridSimulation()
        {
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobBoard = new Queue<Vector2I>();
            _pawnPosition = new Vector2I(0, 0);
            _currentPath = new List<Vector2I>();
            _isBusy = false;
            
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

        public void AddJob(Vector2I consolePosition)
        {
            if (_grid.ContainsKey(consolePosition))
            {
                TileDefinition tileDef = TileRegistry.Get(_grid[consolePosition].Type);
                if (tileDef != null && tileDef.IsInteractable)
                {
                    _jobBoard.Enqueue(consolePosition);
                    GameLogger.Debug($"Job added to board: {consolePosition}");
                }
            }
        }

        public void Tick()
        {
            // If pawn is idle and there are jobs, assign one
            if (!_isBusy && _jobBoard.Count > 0)
            {
                Vector2I target = _jobBoard.Dequeue();
                CalculatePath(target);
            }
            
            // If pawn has a path, move one step
            if (_currentPath.Count > 0)
            {
                MoveAlongPath();
            }
        }

        private void CalculatePath(Vector2I target)
        {
            var godotPath = _pathfinding.GetIdPath(_pawnPosition, target);
            _currentPath = new List<Vector2I>(godotPath);
            if (_currentPath.Count > 0)
            {
                _isBusy = true;
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
                
                // If path is complete, mark as idle
                if (_currentPath.Count == 0)
                {
                    _isBusy = false;
                    GameLogger.Debug("Pawn completed job, now idle");
                }
            }
        }

        public bool TryMovePawn(Vector2I target)
        {
            // This method is deprecated - use job system instead
            return false;
        }
    }
}
