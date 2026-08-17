using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Server
{
    public class GridSimulation
    {
        private Dictionary<Vector2I, GridTileData> _grid;
        private Vector2I _pawnPosition;

        public GridSimulation()
        {
            _grid = new Dictionary<Vector2I, GridTileData>();
            _pawnPosition = new Vector2I(0, 0);
            InitializeGrid();
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
        }

        public Dictionary<Vector2I, GridTileData> GetGrid()
        {
            return new Dictionary<Vector2I, GridTileData>(_grid);
        }

        public Vector2I GetPawnPosition()
        {
            return _pawnPosition;
        }

        public bool TryMovePawn(Vector2I target)
        {
            if (!_grid.ContainsKey(target))
            {
                return false;
            }

            if (_grid[target].Type != TileType.Deck)
            {
                return false;
            }

            _pawnPosition = target;
            return true;
        }
    }
}
