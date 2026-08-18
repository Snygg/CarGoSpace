using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public class OperateJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            context.SetTileState(job.Target, 1);
            GameLogger.Debug($"Harpoon at {job.Target} set to operating state");
        }
    }
}
