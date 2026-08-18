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

            // Calculate grid bounds to center properly
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            foreach (var coord in grid.Keys)
            {
                minX = Mathf.Min(minX, coord.X);
                maxX = Mathf.Max(maxX, coord.X);
                minY = Mathf.Min(minY, coord.Y);
                maxY = Mathf.Max(maxY, coord.Y);
            }
            
            int gridWidth = (maxX - minX + 1) * Constants.TileSize;
            int gridHeight = (maxY - minY + 1) * Constants.TileSize;
            
            GameLogger.Debug($"Grid bounds: ({minX},{minY}) to ({maxX},{maxY}), size: {gridWidth}x{gridHeight}");

            // Position this Node2D at the center of the screen
            Vector2 viewportCenter = GetViewport().GetVisibleRect().Size / 2;
            Position = viewportCenter - new Vector2(gridWidth / 2f, gridHeight / 2f);

            // Create visuals for each tile
            foreach (var kvp in grid)
            {
                Vector2I gridCoord = kvp.Key;
                GridTileData tileData = kvp.Value;

                ColorRect tileRect = new ColorRect();
                tileRect.Size = new Vector2(Constants.TileSize, Constants.TileSize);
                
                // Set color based on tile type
                tileRect.Color = tileData.Type == TileType.Deck ? Colors.Gray : Colors.Black;
                
                // Position tile relative to this Node2D (which is already centered)
                Vector2 localPosition = new Vector2(
                    (gridCoord.X - minX) * Constants.TileSize,
                    (gridCoord.Y - minY) * Constants.TileSize
                );
                tileRect.Position = localPosition;

                AddChild(tileRect);
                _tileVisuals[gridCoord] = tileRect;
            }

            GameLogger.Debug($"Rendered {_tileVisuals.Count} tiles total, Node2D position: {Position}");
        }
    }
}
