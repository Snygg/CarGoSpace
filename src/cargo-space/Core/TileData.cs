using Godot;

namespace CargoSpace.Core
{
    public enum TileType : byte
    {
        Space = 0,
        Deck = 1,
        Console = 2,
        Harpoon = 3
    }

    public struct GridTileData
    {
        public TileType Type;
        public int State;
        public byte HazardState;

        public GridTileData(TileType type, int state = 0, byte hazardState = 0)
        {
            Type = type;
            State = state;
            HazardState = hazardState;
        }
    }
}
