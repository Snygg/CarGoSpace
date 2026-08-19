using Godot;

namespace CargoSpace.Core
{
    public struct GridTileData
    {
        public byte TypeId;
        public byte SurfaceTypeId;
        public int State;
        public byte HazardState;

        public GridTileData(byte typeId, byte surfaceTypeId = 0, int state = 0, byte hazardState = 0)
        {
            TypeId = typeId;
            SurfaceTypeId = surfaceTypeId;
            State = state;
            HazardState = hazardState;
        }

        public TileDefinition GetEffectiveDefinition()
        {
            return SurfaceTypeId != 0 ? TileRegistry.Get(SurfaceTypeId) : TileRegistry.Get(TypeId);
        }
    }
}
