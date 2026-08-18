using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public class SetStateJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            if (context.IsInteractableTile(job.Target))
            {
                context.SetTileState(job.Target, job.TargetState);
                GameLogger.Debug($"Console at {job.Target} set to state {job.TargetState}");
            }
        }
    }
}
