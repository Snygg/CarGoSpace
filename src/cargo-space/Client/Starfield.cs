using Godot;
using System;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class Starfield : Node2D
    {
        [Export]
        public float WarpSpeed = 1f;

        [Export]
        public int StarCount = 200;

        private List<Vector2> _stars = new List<Vector2>();
        private List<float> _starSizes = new List<float>();
        private Random _random = new Random();
        private Rect2 _fieldBounds = new Rect2(-2000, -2000, 4000, 4000);

        public override void _Ready()
        {
            GenerateStars();
        }

        private void GenerateStars()
        {
            _stars.Clear();
            _starSizes.Clear();

            for (int i = 0; i < StarCount; i++)
            {
                float x = (float)(_random.NextDouble() * _fieldBounds.Size.X) + _fieldBounds.Position.X;
                float y = (float)(_random.NextDouble() * _fieldBounds.Size.Y) + _fieldBounds.Position.Y;
                _stars.Add(new Vector2(x, y));
                _starSizes.Add((float)(_random.NextDouble() * 2.0 + 0.5));
            }
        }

        public override void _Process(double delta)
        {
            Vector2 center = _fieldBounds.GetCenter();

            for (int i = 0; i < _stars.Count; i++)
            {
                Vector2 star = _stars[i];
                Vector2 direction = star - center;
                if (direction == Vector2.Zero)
                {
                    direction = new Vector2(1, 0);
                }

                star += direction.Normalized() * WarpSpeed * (float)_starSizes[i] * (float)delta * 10f;

                if (!_fieldBounds.HasPoint(star))
                {
                    star = new Vector2(
                        (float)(_random.NextDouble() * _fieldBounds.Size.X) + _fieldBounds.Position.X,
                        (float)(_random.NextDouble() * _fieldBounds.Size.Y) + _fieldBounds.Position.Y
                    );
                }

                _stars[i] = star;
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 center = _fieldBounds.GetCenter();

            for (int i = 0; i < _stars.Count; i++)
            {
                Vector2 star = _stars[i];
                Vector2 direction = (star - center).Normalized();
                float length = _starSizes[i] * (1f + WarpSpeed * 2f);
                float width = _starSizes[i];

                DrawLine(star - direction * length * 0.5f, star + direction * length * 0.5f, new Color(1, 1, 1, 0.8f), width);
            }
        }
    }
}
