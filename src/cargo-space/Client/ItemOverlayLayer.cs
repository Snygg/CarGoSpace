using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class ItemOverlayLayer : Node2D
    {
        public Dictionary<Vector2I, List<string>> GroundItemsRef { get; set; }

        public override void _Draw()
        {
            if (GroundItemsRef == null)
            {
                return;
            }

            foreach (var kvp in GroundItemsRef)
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                {
                    continue;
                }

                Vector2 topLeft = new Vector2(
                    kvp.Key.X * Constants.TileSize,
                    kvp.Key.Y * Constants.TileSize
                );
                Vector2 center = topLeft + new Vector2(Constants.TileSize / 2f, Constants.TileSize / 2f);
                float size = Constants.TileSize * 0.4f;
                Vector2 offset = new Vector2(size / 2f, size / 2f);

                DrawRect(
                    new Rect2(center - offset, new Vector2(size, size)),
                    new Color(0.9f, 0.7f, 0.2f)
                );

                // Draw a tiny count for stacked items
                if (kvp.Value.Count > 1)
                {
                    DrawString(ThemeDB.FallbackFont, center + new Vector2(0, 4), kvp.Value.Count.ToString(),
                        fontSize: 10, modulate: Colors.Black);
                }
            }
        }
    }
}
