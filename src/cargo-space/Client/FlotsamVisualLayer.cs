using Godot;
using CargoSpace.Core;
using System;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class FlotsamVisualLayer : Node2D
    {
        [Export]
        public Camera2D CameraRef;

        [Export]
        public int MaxJunkCount = 20;

        [Export]
        public float SpawnRadiusMargin = 1.1f;

        [Export]
        public float DespawnDistance = 1200f;

        [Export]
        public float MinJunkSpeed = 10f;

        [Export]
        public float MaxJunkSpeed = 30f;

        private class DriftingJunk
        {
            public ColorRect Node;
            public Vector2 Velocity;
            public float RotationSpeed;
        }

        private List<DriftingJunk> _activeJunk = new List<DriftingJunk>();
        private Random _random = new Random();

        public override void _Process(double delta)
        {
            if (CameraRef == null)
            {
                return;
            }

            // Maintain a constant field of drifting junk
            while (_activeJunk.Count < MaxJunkCount)
            {
                SpawnJunk();
            }

            float dt = (float)delta;
            Vector2 camPos = CameraRef.GlobalPosition;

            for (int i = _activeJunk.Count - 1; i >= 0; i--)
            {
                DriftingJunk junk = _activeJunk[i];
                junk.Node.Position += junk.Velocity * dt;
                junk.Node.Rotation += junk.RotationSpeed * dt;

                if (junk.Node.GlobalPosition.DistanceTo(camPos) > DespawnDistance)
                {
                    junk.Node.QueueFree();
                    _activeJunk.RemoveAt(i);
                }
            }
        }

        private void SpawnJunk()
        {
            Vector2 spawnPos = GetOffScreenSpawnPosition();
            ColorRect node = new ColorRect
            {
                Size = new Vector2(Constants.TileSize * 0.5f, Constants.TileSize * 0.5f),
                PivotOffset = new Vector2(Constants.TileSize * 0.25f, Constants.TileSize * 0.25f),
                Color = Colors.Silver,
                Position = spawnPos,
                Rotation = (float)(_random.NextDouble() * Math.Tau)
            };
            AddChild(node);

            float angle = (float)(_random.NextDouble() * Math.Tau);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();
            float speed = (float)(_random.NextDouble() * (MaxJunkSpeed - MinJunkSpeed) + MinJunkSpeed);

            DriftingJunk junk = new DriftingJunk
            {
                Node = node,
                Velocity = dir * speed,
                RotationSpeed = (float)((_random.NextDouble() * 2.0 - 1.0) * Math.PI)
            };
            _activeJunk.Add(junk);
        }

        private Vector2 GetOffScreenSpawnPosition()
        {
            Vector2 viewportSize = CameraRef.GetViewport().GetVisibleRect().Size;
            float zoom = CameraRef.Zoom.X; // Assuming uniform zoom
            Vector2 visibleHalf = (viewportSize / zoom) * 0.5f;
            float visibleRadius = visibleHalf.Length();
            float spawnRadius = visibleRadius * SpawnRadiusMargin;

            float angle = (float)(_random.NextDouble() * Math.Tau);
            return CameraRef.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        }

        public void PlayCatchEffect(Vector2I targetCoord, string itemId)
        {
            Vector2 targetPos = new Vector2(
                targetCoord.X * Constants.TileSize,
                targetCoord.Y * Constants.TileSize
            );

            // Ensure there is at least one piece of junk to intercept
            if (_activeJunk.Count == 0)
            {
                SpawnJunkNear(targetPos);
            }

            // Find the nearest drifting junk to the harpoon target
            DriftingJunk nearest = null;
            float nearestDist = float.MaxValue;
            foreach (DriftingJunk junk in _activeJunk)
            {
                float dist = junk.Node.GlobalPosition.DistanceTo(targetPos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = junk;
                }
            }

            if (nearest == null)
            {
                return;
            }

            // Stop its drift and intercept the harpoon
            _activeJunk.Remove(nearest);
            nearest.Velocity = Vector2.Zero;
            nearest.RotationSpeed = 0f;

            Tween tween = CreateTween();
            tween.TweenProperty(nearest.Node, "position", targetPos, 0.4f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(nearest.Node.QueueFree));
        }

        private void SpawnJunkNear(Vector2 position)
        {
            Vector2 spawnPos = position + new Vector2(
                (float)(_random.NextDouble() * 200 - 100),
                (float)(_random.NextDouble() * 200 - 100)
            );

            ColorRect node = new ColorRect
            {
                Size = new Vector2(Constants.TileSize * 0.5f, Constants.TileSize * 0.5f),
                PivotOffset = new Vector2(Constants.TileSize * 0.25f, Constants.TileSize * 0.25f),
                Color = Colors.Silver,
                Position = spawnPos,
                Rotation = (float)(_random.NextDouble() * Math.Tau)
            };
            AddChild(node);

            _activeJunk.Add(new DriftingJunk
            {
                Node = node,
                Velocity = Vector2.Zero,
                RotationSpeed = 0f
            });
        }
    }
}
