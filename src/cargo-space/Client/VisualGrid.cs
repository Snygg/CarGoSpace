using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class VisualGrid : Node2D
    {
        private TileMapLayer _tileMapLayer;

        public override void _Ready()
        {
            // Create and configure the TileMapLayer
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.TileSet = CreateTileSet();
            AddChild(_tileMapLayer);
        }

        public void RenderGrid(Dictionary<Vector2I, GridTileData> grid)
        {
            GameLogger.Debug($"RenderGrid called with {grid.Count} tiles");
            
            // Clear existing tiles
            _tileMapLayer.Clear();

            // Keep this Node2D at world origin
            Position = Vector2.Zero;

            // Set tiles based on grid data
            foreach (var kvp in grid)
            {
                Vector2I gridCoord = kvp.Key;
                GridTileData tileData = kvp.Value;
                
                // Set the cell at the grid coordinate with the tile type's atlas coords
                _tileMapLayer.SetCell(gridCoord, (int)tileData.Type, new Vector2I(0, 0));
            }

            GameLogger.Debug($"Rendered {_tileMapLayer.GetUsedCells().Count} tiles total at world coordinates");
        }

        private TileSet CreateTileSet()
        {
            TileSet tileSet = new TileSet();
            
            // Configure tile size
            tileSet.TileSize = new Vector2I(Constants.TileSize, Constants.TileSize);
            
            // Create an atlas source for each tile type
            foreach (TileDefinition tileDef in TileRegistry.AllTiles)
            {
                ImageTexture texture = GenerateTexture(tileDef.GetColor());
                TileSetAtlasSource atlasSource = new TileSetAtlasSource();
                atlasSource.Texture = texture;
                atlasSource.TextureRegionSize = new Vector2I(Constants.TileSize, Constants.TileSize);
                atlasSource.CreateTile(new Vector2I(0, 0));
                
                tileSet.AddSource(atlasSource, (int)tileDef.Type);
            }

            return tileSet;
        }

        private ImageTexture GenerateTexture(Color color)
        {
            Image image = Image.Create(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
            image.Fill(color);
            
            ImageTexture texture = ImageTexture.CreateFromImage(image);
            return texture;
        }
    }
}
