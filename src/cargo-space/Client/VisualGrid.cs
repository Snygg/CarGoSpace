using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class VisualGrid : Node2D
    {
        private Dictionary<Vector2I, ColorRect> _tileVisuals = new Dictionary<Vector2I, ColorRect>();

        public void RenderGrid(Dictionary<Vector2I, GridTileData> grid)
        {
            GameLogger.Debug($"RenderGrid called with {grid.Count} tiles");
            
            // Clear existing visuals
            foreach (var tileVisual in _tileVisuals.Values)
            {
                tileVisual.QueueFree();
            }
            _tileVisuals.Clear();

            // Keep this Node2D at world origin
            Position = Vector2.Zero;

            // Create visuals for each tile at world coordinates
            foreach (var kvp in grid)
            {
                Vector2I gridCoord = kvp.Key;
                GridTileData tileData = kvp.Value;

                ColorRect tileRect = new ColorRect();
                tileRect.Size = new Vector2(Constants.TileSize, Constants.TileSize);
                
                // Set color based on tile type
                tileRect.Color = tileData.Type == TileType.Deck ? Colors.Gray : Colors.Black;
                
                // Position tile at world coordinates
                Vector2 worldPosition = new Vector2(
                    gridCoord.X * Constants.TileSize,
                    gridCoord.Y * Constants.TileSize
                );
                tileRect.Position = worldPosition;

                AddChild(tileRect);
                _tileVisuals[gridCoord] = tileRect;
            }

            GameLogger.Debug($"Rendered {_tileVisuals.Count} tiles total at world coordinates");
        }
    }
}
