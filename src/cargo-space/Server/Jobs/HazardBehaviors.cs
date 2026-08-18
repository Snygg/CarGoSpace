using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public class StartFireJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            context.SetTileHazard(job.Target, 1);
            GameLogger.Debug($"Fire started at {job.Target}");
        }
    }

    public class FightFireJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            context.SetTileHazard(job.Target, 0);
            GameLogger.Debug($"Fire fought at {job.Target}");
        }
    }
}
