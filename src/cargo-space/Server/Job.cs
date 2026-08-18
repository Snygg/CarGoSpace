using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public enum JobType : byte
    {
        SetState = 0
    }

    public struct Job
    {
        public JobId Id;
        public long OwnerPeerId;
        public Vector2I Target;
        public JobType Type;
        public int TargetState;

        public Job(JobId id, long ownerPeerId, Vector2I target, JobType type, int targetState)
        {
            Id = id;
            OwnerPeerId = ownerPeerId;
            Target = target;
            Type = type;
            TargetState = targetState;
        }
    }
}
