using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class GridHighlighter : Node2D
    {
        public ClientDataCache DataCache;
        public ClientConstructionValidator Validator;
        public TextureFactory TextureFactory;

        public GridHighlighter(ClientDataCache dataCache, ClientConstructionValidator validator, TextureFactory textureFactory)
        {
            DataCache = dataCache;
            Validator = validator;
            TextureFactory = textureFactory;
        }

        public override void _Draw()
        {
            base._Draw();
            if (DataCache == null) return;

            // Existing blueprint holos
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

                if (DataCache.CurrentInputMode == InputController.InputMode.Blueprint &&
                    DataCache.CurrentBlueprintTargetTypeId != 0)
                {
                    DrawBlueprintGhost(coord, worldPos, rect);
                }
                else
                {
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
        }

        private void DrawBlueprintGhost(Vector2I coord, Vector2 worldPos, Rect2 fallbackRect)
        {
            TileDefinition targetDef = TileRegistry.Get(DataCache.CurrentBlueprintTargetTypeId);
            if (targetDef == null) return;

            bool canPlace = Validator.CanPlaceBlueprint(coord, targetDef);

            Color modulate = canPlace
                ? new Color(1.0f, 1.0f, 1.0f, 0.5f)
                : new Color(1.0f, 0.2f, 0.2f, 0.5f);

            int sourceId = TextureFactory.GetSourceId(targetDef.TypeId, 1);
            TileSet tileSet = TextureFactory.GetTileSet();
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
                DrawRect(fallbackRect, ghostColor);
            }

            Color borderColor = canPlace
                ? new Color(0.2f, 1.0f, 0.4f, 0.8f)
                : new Color(1.0f, 0.2f, 0.2f, 0.8f);
            DrawRect(fallbackRect, borderColor, false, 2.0f);
        }
    }
}
