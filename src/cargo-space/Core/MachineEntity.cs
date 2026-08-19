using Godot;
using System.Collections.Generic;
using System.Text;

namespace CargoSpace.Core;

public class MachineEntity : IGridEntity
{
    public EntityType Type => EntityType.Machine;

    private Vector2I _position;
    public Vector2I Position
    {
        get => _position;
        private set => _position = value;
    }

    public bool IsPositionDirty { get; private set; } = true;

    public Dictionary<string, float> DynamicState { get; set; } = new();

    public MachineEntity(Vector2I pos)
    {
        Position = pos;
    }

    public byte[] GetNetworkIdBytes() => Encoding.UTF8.GetBytes(Position.ToString());

    public void ClearDirtyFlag() => IsPositionDirty = false;
}
