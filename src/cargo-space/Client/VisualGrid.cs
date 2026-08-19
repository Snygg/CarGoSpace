using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class VisualGrid : Node2D
    {
        private TileMapLayer _tileMapLayer;
        private TileMapLayer _surfaceLayer;
        private TileMapLayer _hazardLayer;
        private TileMapLayer _zoneLayer;
        private ItemOverlayLayer _itemLayer;
        private BlueprintPreview _blueprintPreview;
        private Dictionary<Vector2I, List<string>> _groundItems = new();

        public ClientDataCache DataCache;

        public override void _Ready()
        {
            // Create and configure the TileMapLayer
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.TileSet = CreateTileSet();
            _tileMapLayer.ZAsRelative = false;
            _tileMapLayer.ZIndex = -1;
            AddChild(_tileMapLayer);

            // Surface layer sits on top of the floor
            _surfaceLayer = new TileMapLayer();
            _surfaceLayer.TileSet = CreateTileSet();
            _surfaceLayer.ZIndex = 0;
            AddChild(_surfaceLayer);

            // Create the hazard overlay layer on top
            _hazardLayer = new TileMapLayer();
            _hazardLayer.TileSet = CreateHazardTileSet();
            AddChild(_hazardLayer);

            // Create the zone overlay layer between hazards and items
            _zoneLayer = new TileMapLayer();
            _zoneLayer.TileSet = CreateZoneTileSet();
            _zoneLayer.ZIndex = 1;
            AddChild(_zoneLayer);

            // Create the item overlay layer on top of everything
            _itemLayer = new ItemOverlayLayer();
            _itemLayer.GroundItemsRef = _groundItems;
            _itemLayer.ZIndex = 2;
            AddChild(_itemLayer);

            // Create a dedicated preview layer that draws above all tile layers
            _blueprintPreview = new BlueprintPreview { Grid = this };
            _blueprintPreview.ZIndex = 10;
            AddChild(_blueprintPreview);
        }

        public void UpdateGroundItems(Vector2I coord, List<string> items)
        {
            _groundItems[coord] = items;
            _itemLayer?.QueueRedraw();
        }

        public void UpdateBlueprints()
        {
            QueueRedraw();
        }

        public void UpdateTile(Vector2I coord, GridTileData tileData)
        {
            int sourceId = GetSourceId(tileData.TypeId, tileData.State);
            _tileMapLayer?.SetCell(coord, sourceId, new Vector2I(0, 0));

            if (tileData.SurfaceTypeId != 0)
            {
                int surfaceSourceId = GetSourceId(tileData.SurfaceTypeId, tileData.State);
                _surfaceLayer?.SetCell(coord, surfaceSourceId, new Vector2I(0, 0));
            }
            else
            {
                _surfaceLayer?.EraseCell(coord);
            }

            if (tileData.HazardState == 1)
            {
                _hazardLayer?.SetCell(coord, 1, new Vector2I(0, 0));
            }
            else
            {
                _hazardLayer?.EraseCell(coord);
            }
        }

        public void UpdateZones(IReadOnlyDictionary<Vector2I, ZoneType> zoneTiles)
        {
            _zoneLayer?.Clear();

            if (zoneTiles == null)
            {
                return;
            }

            foreach (var kvp in zoneTiles)
            {
                if (kvp.Value == ZoneType.None)
                    continue;

                int sourceId = GetZoneSourceId(kvp.Value);
                _zoneLayer?.SetCell(kvp.Key, sourceId, new Vector2I(0, 0));
            }
        }

        public new void QueueRedraw()
        {
            base.QueueRedraw();
            _blueprintPreview?.QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            if (DataCache == null) return;

            foreach (var kvp in DataCache.Blueprints)
            {
                Vector2I coord = kvp.Key;
                Blueprint bp = kvp.Value;
                TileDefinition targetDef = TileRegistry.Get(bp.TargetTypeId);
                if (targetDef == null) continue;

                Color baseColor = targetDef.GetColor();
                Color hologramColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.5f); // 50% opacity

                Vector2 worldPos = new Vector2(coord.X * Constants.TileSize, coord.Y * Constants.TileSize);
                Rect2 rect = new Rect2(worldPos, new Vector2(Constants.TileSize, Constants.TileSize));

                // Zero-allocation, hardware-accelerated rectangle drawing!
                DrawRect(rect, hologramColor);
            }

            // Hover preview highlight
            if (DataCache.HoveredTile.HasValue)
            {
                Vector2I coord = DataCache.HoveredTile.Value;
                Vector2 worldPos = new Vector2(coord.X * Constants.TileSize, coord.Y * Constants.TileSize);

                // Default: subtle grey outline with a little padding around the tile
                float padding = 2.0f;
                Rect2 rect = new Rect2(
                    worldPos - new Vector2(padding, padding),
                    new Vector2(Constants.TileSize + padding * 2, Constants.TileSize + padding * 2));

                Color fillColor;
                Color borderColor;

                if (DataCache.CurrentInputMode == InputController.InputMode.PaintingZone)
                {
                    fillColor = new Color(0.2f, 0.5f, 1.0f, 0.12f);
                    borderColor = new Color(0.2f, 0.5f, 1.0f, 0.6f);
                }
                else
                {
                    fillColor = new Color(0.8f, 0.8f, 0.8f, 0.08f);
                    borderColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);
                }

                DrawRect(rect, fillColor);
                DrawRect(rect, borderColor, false, 1.0f);
            }
        }

        public void RenderGrid(IReadOnlyDictionary<Vector2I, GridTileData> grid)
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

                // Render surface overlay
                if (tileData.SurfaceTypeId != 0)
                {
                    int surfaceSourceId = GetSourceId(tileData.SurfaceTypeId, tileData.State);
                    _surfaceLayer.SetCell(gridCoord, surfaceSourceId, new Vector2I(0, 0));
                }
                else
                {
                    _surfaceLayer.EraseCell(gridCoord);
                }

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

        private int GetSourceId(byte typeId, int state)
        {
            return typeId * 10 + state;
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

        private int GetZoneSourceId(ZoneType zoneType)
        {
            return (int)zoneType;
        }

        private bool CanPlaceBlueprint(Vector2I coord, TileDefinition targetDef)
        {
            if (DataCache == null || targetDef == null)
                return false;

            if (DataCache.Blueprints.ContainsKey(coord))
                return false;

            if (!DataCache.TryGetTile(coord, out GridTileData currentTile))
                return false;

            if (targetDef.Layer == "Floor")
            {
                if (currentTile.SurfaceTypeId != 0)
                    return false;

                TileDefinition currentDef = TileRegistry.Get(currentTile.TypeId);
                if (currentDef == null)
                    return false;

                if (currentDef.Layer == "Floor")
                    return true;

                if (currentDef.Layer == "Base")
                    return IsAdjacentToShip(coord);

                return false;
            }
            else if (targetDef.Layer == "Surface")
            {
                TileDefinition currentDef = currentTile.GetEffectiveDefinition();
                if (currentDef == null || currentTile.SurfaceTypeId != 0)
                    return false;

                // Surfaces can be built on a floor, or on a space tile that
                // touches the ship so you can place a hull/wall around the edge.
                if (currentDef.Layer == "Floor")
                    return true;

                if (currentDef.Layer == "Base")
                    return IsAdjacentToShip(coord);

                return false;
            }

            return false;
        }

        private bool IsAdjacentToShip(Vector2I coord)
        {
            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I neighbor = coord + dir;
                if (DataCache.TryGetTile(neighbor, out GridTileData tile))
                {
                    TileDefinition def = tile.GetEffectiveDefinition();
                    if (def != null && (def.Layer == "Floor" || def.Layer == "Surface"))
                        return true;
                }
            }
            return false;
        }

        private partial class BlueprintPreview : Node2D
        {
            public VisualGrid Grid;

            public override void _Draw()
            {
                base._Draw();

                ClientDataCache data = Grid?.DataCache;
                if (data == null) return;
                if (data.CurrentInputMode != InputController.InputMode.Blueprint) return;
                if (!data.HoveredTile.HasValue) return;

                byte targetTypeId = data.CurrentBlueprintTargetTypeId;
                if (targetTypeId == 0) return;

                TileDefinition targetDef = TileRegistry.Get(targetTypeId);
                if (targetDef == null) return;

                Vector2I coord = data.HoveredTile.Value;
                bool canPlace = Grid.CanPlaceBlueprint(coord, targetDef);

                Color modulate = canPlace
                    ? new Color(1.0f, 1.0f, 1.0f, 0.5f)
                    : new Color(1.0f, 0.2f, 0.2f, 0.5f);

                Vector2 worldPos = new Vector2(coord.X * Constants.TileSize, coord.Y * Constants.TileSize);
                Rect2 rect = new Rect2(worldPos, new Vector2(Constants.TileSize, Constants.TileSize));

                int sourceId = Grid.GetSourceId(targetDef.TypeId, 1);
                TileSet tileSet = Grid._tileMapLayer?.TileSet;
                if (tileSet != null && tileSet.GetSource(sourceId) is TileSetAtlasSource atlasSource && atlasSource.Texture != null)
                {
                    DrawTexture(atlasSource.Texture, worldPos, modulate);
                }
                else
                {
                    // Fallback: color rectangle if texture is unavailable
                    Color ghostColor = canPlace
                        ? new Color(0.2f, 1.0f, 0.4f, 0.35f)
                        : new Color(1.0f, 0.2f, 0.2f, 0.35f);
                    DrawRect(rect, ghostColor);
                }

                Color borderColor = canPlace
                    ? new Color(0.2f, 1.0f, 0.4f, 0.8f)
                    : new Color(1.0f, 0.2f, 0.2f, 0.8f);
                DrawRect(rect, borderColor, false, 2.0f);
            }
        }
    }
}
