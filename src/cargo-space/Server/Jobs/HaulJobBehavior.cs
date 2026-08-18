using CargoSpace.Core;
using Godot;

namespace CargoSpace.Server
{
    public class HaulJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            if (string.IsNullOrEmpty(job.ItemId))
            {
                GameLogger.Warning($"Haul job {job.Id} has no ItemId");
                return;
            }

            GameLogger.Debug($"Hauling {job.ItemId} from {job.Target} to {job.Destination}");

            if (!context.RemoveItemFromGrid(job.Target, job.ItemId))
            {
                GameLogger.Warning($"Haul job {job.Id}: no {job.ItemId} at {job.Target}");
                return;
            }

            context.AddItemToGrid(job.Destination, job.ItemId);
        }
    }
}
