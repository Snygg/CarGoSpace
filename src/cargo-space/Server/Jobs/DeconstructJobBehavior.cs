using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server.Jobs
{
    public class DeconstructJobBehavior : IJobBehavior
    {
        public void Execute(Job job, IJobExecutionContext context)
        {
            if (!context.TryGetTile(job.Target, out GridTileData tileData))
                return;

            TileDefinition def;

            if (tileData.SurfaceTypeId != 0)
            {
                // Remove the surface object; the floor remains
                def = TileRegistry.Get(tileData.SurfaceTypeId);
                context.SetSurfaceType(job.Target, 0);

                if (def?.DeconstructYield != null)
                {
                    foreach (var kvp in def.DeconstructYield)
                    {
                        for (int i = 0; i < kvp.Value; i++)
                        {
                            context.SpawnItemToGrid(job.Target, kvp.Key);
                        }
                    }
                }
            }
            else
            {
                // Remove the floor and revert to its DeconstructInto type
                def = TileRegistry.Get(tileData.TypeId);
                byte nextType = def?.DeconstructInto ?? 0;
                context.SetTileType(job.Target, nextType);

                if (def?.DeconstructYield != null)
                {
                    foreach (var kvp in def.DeconstructYield)
                    {
                        for (int i = 0; i < kvp.Value; i++)
                        {
                            context.SpawnItemToGrid(job.Target, kvp.Key);
                        }
                    }
                }
            }
        }
    }
}
