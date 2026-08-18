using CargoSpace.Core;
using Godot;
using System.Collections.Generic;

namespace CargoSpace.Server;

public class Pawn
{
    public PawnId Id { get; }
    public Vector2I Position { get; set; }
    public List<Vector2I> CurrentPath { get; set; } = new();
    public PawnState State { get; set; } = PawnState.Idle;
    public Job CurrentJob { get; set; }
    public int WorkTicksRemaining { get; set; } = 0;

    public Pawn(PawnId id, Vector2I startingPosition)
    {
        Id = id;
        Position = startingPosition;
    }
}
