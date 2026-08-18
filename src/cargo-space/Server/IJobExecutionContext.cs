using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public interface IJobExecutionContext
    {
        bool IsInteractableTile(Vector2I target);
        void SetTileState(Vector2I target, int state);
        void SetTileHazard(Vector2I target, byte hazardState);
        void BroadcastTileUpdate(Vector2I target, GridTileData data);
    }
}
