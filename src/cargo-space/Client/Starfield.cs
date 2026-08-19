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

                float size = (float)(_random.NextDouble() * 2.0 + 0.5);
                _starSizes.Add(size);
            }
        }

        public override void _Process(double delta)
        {
            Vector2 moveDir = Direction == Vector2.Zero ? new Vector2(1, 0) : Direction.Normalized();
            float dt = (float)delta;

            for (int i = 0; i < _stars.Count; i++)
            {
                // Parallax: larger stars are closer and move faster as a factor of the given speed
                float parallax = 0.2f + _starSizes[i] * 0.4f;
                _stars[i] += moveDir * Speed * parallax * dt;
                _stars[i] = WrapPosition(_stars[i]);
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
                float size = _starSizes[i];
                float alpha = Mathf.Clamp(0.4f + size * 0.2f, 0.5f, 1.0f);
                DrawCircle(_stars[i], size * 0.5f, new Color(1, 1, 1, alpha));
            }
        }
    }
}
