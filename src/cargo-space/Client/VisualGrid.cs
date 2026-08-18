using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class VisualGrid : Node2D
    {
        private TileMapLayer _tileMapLayer;
        private TileMapLayer _hazardLayer;

        public override void _Ready()
        {
            // Create and configure the TileMapLayer
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.TileSet = CreateTileSet();
            AddChild(_tileMapLayer);

            // Create the hazard overlay layer on top
            _hazardLayer = new TileMapLayer();
            _hazardLayer.TileSet = CreateHazardTileSet();
            AddChild(_hazardLayer);
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
                int sourceId = GetSourceId(tileData.TypeId, tileData.State);

                // Set the cell at the grid coordinate with the tile type's atlas coords
                _tileMapLayer.SetCell(gridCoord, sourceId, new Vector2I(0, 0));

                // Render hazard overlay
                if (tileData.HazardState == 1)
                {
                    _hazardLayer.SetCell(gridCoord, 1, new Vector2I(0, 0));
                }
                else
                {
                    _hazardLayer.EraseCell(gridCoord);
                }
            }

            GameLogger.Debug($"Rendered {_tileMapLayer.GetUsedCells().Count} tiles total at world coordinates");
        }

        private TileSet CreateTileSet()
        {
            TileSet tileSet = new TileSet();
            
            // Configure tile size
            tileSet.TileSize = new Vector2I(Constants.TileSize, Constants.TileSize);
            
            // Create atlas sources for each tile type and state combination
            foreach (TileDefinition tileDef in TileRegistry.AllTiles)
            {
                // State 0 (default/off)
                AddAtlasSource(tileSet, tileDef, 0);
                
                // State 1 (on) for interactable tiles
                if (tileDef.IsInteractable)
                {
                    AddAtlasSource(tileSet, tileDef, 1);
                }
            }

            return tileSet;
        }

        private void AddAtlasSource(TileSet tileSet, TileDefinition tileDef, int state)
        {
            Color color = GetRenderColor(tileDef, state);
            ImageTexture texture = GenerateTexture(color);
            TileSetAtlasSource atlasSource = new TileSetAtlasSource();
            atlasSource.Texture = texture;
            atlasSource.TextureRegionSize = new Vector2I(Constants.TileSize, Constants.TileSize);
            atlasSource.CreateTile(new Vector2I(0, 0));
            
            int sourceId = GetSourceId(tileDef.TypeId, state);
            tileSet.AddSource(atlasSource, sourceId);
        }

        private int GetSourceId(byte typeId, int state)
        {
            return typeId * 2 + state;
        }

        private TileSet CreateHazardTileSet()
        {
            TileSet tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(Constants.TileSize, Constants.TileSize);

            ImageTexture texture = GenerateHazardTexture();
            TileSetAtlasSource atlasSource = new TileSetAtlasSource();
            atlasSource.Texture = texture;
            atlasSource.TextureRegionSize = new Vector2I(Constants.TileSize, Constants.TileSize);
            atlasSource.CreateTile(new Vector2I(0, 0));
            tileSet.AddSource(atlasSource, 1);

            return tileSet;
        }

        private ImageTexture GenerateHazardTexture()
        {
            Image image = Image.Create(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
            image.Fill(new Color(0, 0, 0, 0));

            Vector2I center = new Vector2I(Constants.TileSize / 2, Constants.TileSize / 2);
            int radius = Constants.TileSize / 3;
            Color outerColor = new Color(1.0f, 0.2f, 0.0f, 0.85f);
            Color innerColor = new Color(1.0f, 0.8f, 0.1f, 0.95f);

            for (int x = 0; x < Constants.TileSize; x++)
            {
                for (int y = 0; y < Constants.TileSize; y++)
                {
                    float distance = new Vector2(x, y).DistanceTo(center);
                    if (distance <= radius)
                    {
                        float t = distance / radius;
                        Color pixelColor = innerColor.Lerp(outerColor, t);
                        image.SetPixel(x, y, pixelColor);
                    }
                }
            }

            return ImageTexture.CreateFromImage(image);
        }

        private Color GetRenderColor(TileDefinition tileDef, int state)
        {
            Color baseColor = tileDef.GetColor();
            
            // If the tile is a Console and state is 0 (OFF), darken it
            if (tileDef.IsInteractable && state == 0)
            {
                return new Color(baseColor.R * 0.5f, baseColor.G * 0.5f, baseColor.B * 0.5f, baseColor.A);
            }
            
            return baseColor;
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
