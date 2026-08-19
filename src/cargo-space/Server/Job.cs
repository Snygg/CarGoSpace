using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public enum JobType : byte
    {
        SetState = 0,
        StartFire = 1,
        FightFire = 2,
        Operate = 3,
        Haul = 4,
        Supply = 5,
        Construct = 6
    }

    public struct Job
    {
        public JobId Id;
        public long OwnerPeerId;
        public Vector2I Target;
        public Vector2I Destination;
        public JobType Type;
        public int TargetState;
        public string ItemId;

        public Job(JobId id, long ownerPeerId, Vector2I target, JobType type, int targetState)
        {
            Id = id;
            OwnerPeerId = ownerPeerId;
            Target = target;
            Destination = default;
            Type = type;
            TargetState = targetState;
            ItemId = string.Empty;
        }

        public Job(JobId id, long ownerPeerId, Vector2I target, Vector2I destination, JobType type, int targetState, string itemId = "")
        {
            Id = id;
            OwnerPeerId = ownerPeerId;
            Target = target;
            Destination = destination;
            Type = type;
            TargetState = targetState;
            ItemId = itemId;
        }
    }
}
