using Godot;

namespace CargoSpace.Server
{
    public enum JobType : byte
    {
        ToggleState = 0
    }

    public struct Job
    {
        public Vector2I Target;
        public JobType Type;

        public Job(Vector2I target, JobType type)
        {
            Target = target;
            Type = type;
        }
    }
}
