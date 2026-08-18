using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class HarpoonLayer : Node2D
    {
        public Dictionary<Vector2I, GridTileData> GridRef;

        public void PlayCatchEffect(Vector2I targetCoord, string itemId)
        {
            ColorRect scrap = new ColorRect
            {
                Size = new Vector2(Constants.TileSize * 0.5f, Constants.TileSize * 0.5f),
                Color = Colors.Silver
            };
            AddChild(scrap);

            Vector2 endPos = new Vector2(
                targetCoord.X * Constants.TileSize,
                targetCoord.Y * Constants.TileSize
            );
            Vector2 startPos = endPos + new Vector2(800, -800); // Spawn off-screen top-right
            scrap.Position = startPos;

            Tween tween = CreateTween();
            tween.TweenProperty(scrap, "position", endPos, 0.5f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(scrap.QueueFree)); // Destroy after tween
        }

        public override void _Draw()
        {
            if (GridRef == null)
            {
                return;
            }

            foreach (var kvp in GridRef)
            {
                Vector2I coord = kvp.Key;
                GridTileData tileData = kvp.Value;

                if (TileRegistry.Get(tileData.TypeId)?.StringId != "harpoon")
                {
                    continue;
                }

                Vector2 center = new Vector2(
                    coord.X * Constants.TileSize + Constants.TileSize / 2f,
                    coord.Y * Constants.TileSize + Constants.TileSize / 2f
                );
                float halfSize = Constants.TileSize / 2f;

                // Draw a dark triangular turret base
                Vector2[] basePoints = new Vector2[]
                {
                    center + new Vector2(0, -halfSize * 0.7f),
                    center + new Vector2(-halfSize * 0.6f, halfSize * 0.5f),
                    center + new Vector2(halfSize * 0.6f, halfSize * 0.5f)
                };
                DrawColoredPolygon(basePoints, new Color(0.3f, 0.3f, 0.35f));

                // Operating indicator: bright green laser beam
                if (tileData.State == 1)
                {
                    Vector2 beamStart = center + new Vector2(0, -halfSize * 0.5f);
                    Vector2 beamEnd = center + new Vector2(0, -halfSize * 2.5f);
                    DrawLine(beamStart, beamEnd, new Color(0.2f, 1.0f, 0.3f), 4f);

                    // Glow halo
                    DrawCircle(center, halfSize * 0.7f, new Color(0.2f, 1.0f, 0.3f, 0.25f));
                }
            }
        }
    }
}
