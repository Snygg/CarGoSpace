using Godot;

namespace CargoSpace.Core
{
    public struct GridTileData
    {
        public byte TypeId;
        public int State;
        public byte HazardState;

        public GridTileData(byte typeId, int state = 0, byte hazardState = 0)
        {
            TypeId = typeId;
            State = state;
            HazardState = hazardState;
        }
    }
}
