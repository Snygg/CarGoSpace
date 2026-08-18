using Godot;

namespace CargoSpace.Core;

public interface IGridEntity
{
    EntityType Type { get; }
    byte[] GetNetworkIdBytes();
    Vector2I Position { get; }
    bool IsPositionDirty { get; }
    void ClearDirtyFlag();
}
