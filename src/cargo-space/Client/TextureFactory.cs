using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public class TextureFactory
    {
        private TileSet _tileSet;
        private TileSet _hazardTileSet;
        private TileSet _zoneTileSet;

        public TextureFactory()
        {
            _tileSet = CreateTileSet();
            _hazardTileSet = CreateHazardTileSet();
            _zoneTileSet = CreateZoneTileSet();
        }

        public TileSet GetTileSet() => _tileSet;
        public TileSet GetHazardTileSet() => _hazardTileSet;
        public TileSet GetZoneTileSet() => _zoneTileSet;

        public int GetSourceId(byte typeId, int state)
        {
            return typeId * 10 + state;
        }

        public int GetZoneSourceId(ZoneType zoneType)
        {
            return (int)zoneType;
        }

        private TileSet CreateTileSet()
        {
            TileSet tileSet = new TileSet();

            // Configure tile size
            tileSet.TileSize = new Vector2I(Constants.TileSize, Constants.TileSize);

            // Create atlas sources for each tile type and state combination
            foreach (TileDefinition tileDef in TileRegistry.AllTiles)
            {
                // State 0 (default/off) and state 1 (on/built) for every tile,
                // so non-interactable constructions like walls render after SetTileState(1).
                AddAtlasSource(tileSet, tileDef, 0);
                AddAtlasSource(tileSet, tileDef, 1);
            }

            return tileSet;
        }

        private void AddAtlasSource(TileSet tileSet, TileDefinition tileDef, int state)
        {
            ImageTexture texture = GenerateTexture(tileDef, state);
            TileSetAtlasSource atlasSource = new TileSetAtlasSource();
            atlasSource.Texture = texture;
            atlasSource.TextureRegionSize = new Vector2I(Constants.TileSize, Constants.TileSize);
            atlasSource.CreateTile(new Vector2I(0, 0));

            int sourceId = GetSourceId(tileDef.TypeId, state);
            tileSet.AddSource(atlasSource, sourceId);
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
            Image image = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
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
            Image image = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
            image.Fill(color);

            ImageTexture texture = ImageTexture.CreateFromImage(image);
            return texture;
        }

        private ImageTexture GenerateTexture(TileDefinition tileDef, int state)
        {
            Color color = GetRenderColor(tileDef, state);
            Image image = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
            image.Fill(color);

            // Draw a black border on wall tiles so they stand out from the deck floor
            if (tileDef.StringId == "wall")
            {
                Color borderColor = Colors.Black;
                int borderThickness = 2;

                for (int y = 0; y < Constants.TileSize; y++)
                {
                    for (int t = 0; t < borderThickness; t++)
                    {
                        image.SetPixel(t, y, borderColor);
                        image.SetPixel(Constants.TileSize - 1 - t, y, borderColor);
                    }
                }

                for (int x = 0; x < Constants.TileSize; x++)
                {
                    for (int t = 0; t < borderThickness; t++)
                    {
                        image.SetPixel(x, t, borderColor);
                        image.SetPixel(x, Constants.TileSize - 1 - t, borderColor);
                    }
                }
            }
            else if (tileDef.StringId == "fusion_generator")
            {
                // Bright inner circle so the generator does not look like a wall
                Vector2I center = new Vector2I(Constants.TileSize / 2, Constants.TileSize / 2);
                int radius = Constants.TileSize / 4;
                Color glow = new Color(1.0f, 0.95f, 0.3f, 1.0f);

                for (int x = 0; x < Constants.TileSize; x++)
                {
                    for (int y = 0; y < Constants.TileSize; y++)
                    {
                        if (new Vector2(x, y).DistanceTo(center) <= radius)
                            image.SetPixel(x, y, glow);
                    }
                }
            }

            ImageTexture texture = ImageTexture.CreateFromImage(image);
            return texture;
        }

        private TileSet CreateZoneTileSet()
        {
            TileSet tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(Constants.TileSize, Constants.TileSize);

            foreach (ZoneType zoneType in System.Enum.GetValues<ZoneType>())
            {
                if (zoneType == ZoneType.None)
                    continue;

                ImageTexture texture = GenerateZoneTexture(zoneType);
                TileSetAtlasSource atlasSource = new TileSetAtlasSource();
                atlasSource.Texture = texture;
                atlasSource.TextureRegionSize = new Vector2I(Constants.TileSize, Constants.TileSize);
                atlasSource.CreateTile(new Vector2I(0, 0));
                tileSet.AddSource(atlasSource, GetZoneSourceId(zoneType));
            }

            return tileSet;
        }

        private ImageTexture GenerateZoneTexture(ZoneType zoneType)
        {
            Color baseColor = GetZoneBaseColor(zoneType);
            Color fillColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.12f);
            Color borderColor = new Color(baseColor.R, baseColor.G, baseColor.B, 1.0f);

            Image image = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
            image.Fill(fillColor);

            int borderInset = 4;
            int dashLength = 8;
            int dashGap = 8;

            // Top and bottom dashed borders
            for (int x = borderInset; x < Constants.TileSize - borderInset; x++)
            {
                int pos = x - borderInset;
                if ((pos % (dashLength + dashGap)) < dashLength)
                {
                    image.SetPixel(x, borderInset, borderColor);
                    image.SetPixel(x, Constants.TileSize - 1 - borderInset, borderColor);
                }
            }

            // Left and right dashed borders
            for (int y = borderInset; y < Constants.TileSize - borderInset; y++)
            {
                int pos = y - borderInset;
                if ((pos % (dashLength + dashGap)) < dashLength)
                {
                    image.SetPixel(borderInset, y, borderColor);
                    image.SetPixel(Constants.TileSize - 1 - borderInset, y, borderColor);
                }
            }

            return ImageTexture.CreateFromImage(image);
        }

        private Color GetZoneBaseColor(ZoneType zoneType)
        {
            return zoneType switch
            {
                ZoneType.Storage => new Color(0.2f, 0.5f, 1.0f, 1.0f),
                _ => new Color(1.0f, 1.0f, 1.0f, 0.0f)
            };
        }
    }
}
