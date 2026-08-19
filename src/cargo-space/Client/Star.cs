using Godot;

namespace CargoSpace.Client
{
    public readonly struct Star
    {
        public Vector2 Position { get; }
        public float Size { get; }
        public Color Color { get; }
        public float ParallaxFactor { get; }

        public Star(Vector2 position, float size, Color color)
        {
            Position = position;
            Size = size;
            Color = color;
            ParallaxFactor = 0.2f + size * 0.4f;
        }

        public Star WithPosition(Vector2 position)
        {
            return new Star(position, Size, Color);
        }
    }
}
