using CargoSpace.Core;
using Godot;
using System.Collections.Generic;

namespace CargoSpace.Server;

public class Pawn : IGridEntity
{
    public PawnId Id { get; }
    public EntityType Type => EntityType.Pawn;

    private Vector2I _position;
    public Vector2I Position
    {
        get => _position;
        private set => _position = value;
    }

    public List<Vector2I> CurrentPath { get; set; } = new();
    public PawnState State { get; set; } = PawnState.Idle;
    public Job CurrentJob { get; set; }
    public int WorkTicksRemaining { get; set; } = 0;

    public bool IsPositionDirty { get; private set; } = true;

    public Pawn(PawnId id, Vector2I startingPosition)
    {
        Id = id;
        _position = startingPosition;
    }

    public void UpdatePosition(Vector2I newPosition)
    {
        if (_position != newPosition)
        {
            _position = newPosition;
            IsPositionDirty = true;
        }
    }

    public byte[] GetNetworkIdBytes() => Id.ToNetworkBytes();

    public void ClearDirtyFlag() => IsPositionDirty = false;
}
