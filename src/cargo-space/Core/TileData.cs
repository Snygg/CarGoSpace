using Godot;

namespace CargoSpace.Core
{
    public enum TileType : byte
    {
        Space = 0,
        Deck = 1
    }

    public struct GridTileData
    {
        public TileType Type;

        public GridTileData(TileType type)
        {
            Type = type;
        }
    }
}
