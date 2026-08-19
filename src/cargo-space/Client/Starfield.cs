using Godot;
using System;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class Starfield : Node2D
    {
        [Export]
        public float Speed = 30f;

        [Export]
        public Vector2 Direction = new Vector2(0, 1);

        [Export]
        public int StarCount = 200;

        private List<Star> _stars = new List<Star>();
        private Random _random = new Random();
        private Rect2 _fieldBounds = new Rect2(-2000, -2000, 4000, 4000);

        public override void _Ready()
        {
            GenerateStars();
        }

        private void GenerateStars()
        {
            _stars.Clear();

            Color[] tints = new Color[]
            {
                new Color(1.0f, 0.6f, 0.6f), // pale red
                new Color(0.6f, 0.8f, 1.0f), // pale blue
                new Color(1.0f, 0.9f, 0.6f)  // pale yellow
            };

            for (int i = 0; i < StarCount; i++)
            {
                float x = (float)(_random.NextDouble() * _fieldBounds.Size.X) + _fieldBounds.Position.X;
                float y = (float)(_random.NextDouble() * _fieldBounds.Size.Y) + _fieldBounds.Position.Y;
                Vector2 position = new Vector2(x, y);

                float size = (float)(_random.NextDouble() * 2.0 + 0.5);

                Color color = new Color(1, 1, 1);
                if (_random.NextDouble() < 0.08)
                {
                    color = tints[_random.Next(tints.Length)];
                }

                _stars.Add(new Star(position, size, color));
            }
        }

        public override void _Process(double delta)
        {
            Vector2 moveDir = Direction == Vector2.Zero ? new Vector2(1, 0) : Direction.Normalized();
            float dt = (float)delta;

            for (int i = 0; i < _stars.Count; i++)
            {
                // Parallax: larger stars are closer and move faster as a factor of the given speed
                Star star = _stars[i];
                Vector2 newPosition = star.Position + moveDir * Speed * star.ParallaxFactor * dt;
                _stars[i] = star.WithPosition(WrapPosition(newPosition));
            }

            QueueRedraw();
        }

        private Vector2 WrapPosition(Vector2 position)
        {
            float minX = _fieldBounds.Position.X;
            float maxX = _fieldBounds.Position.X + _fieldBounds.Size.X;
            float minY = _fieldBounds.Position.Y;
            float maxY = _fieldBounds.Position.Y + _fieldBounds.Size.Y;

            if (position.X < minX) position.X += _fieldBounds.Size.X;
            if (position.X >= maxX) position.X -= _fieldBounds.Size.X;
            if (position.Y < minY) position.Y += _fieldBounds.Size.Y;
            if (position.Y >= maxY) position.Y -= _fieldBounds.Size.Y;

            return position;
        }

        public override void _Draw()
        {
            DrawRect(_fieldBounds, new Color(0, 0, 0, 1), true);

            for (int i = 0; i < _stars.Count; i++)
            {
                Star star = _stars[i];
                float alpha = Mathf.Clamp(0.4f + star.Size * 0.2f, 0.5f, 1.0f);

                Color color = star.Color;
                color.A = alpha;
                DrawCircle(star.Position, star.Size * 0.5f, color);
            }
        }
    }
}
