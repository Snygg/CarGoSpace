using Godot;
using CargoSpace.Core;

namespace CargoSpace.Server
{
    public interface IJobExecutionContext
    {
        bool IsInteractableTile(Vector2I target);
        bool TryGetTile(Vector2I target, out GridTileData data);
        void SetTileType(Vector2I target, byte typeId);
        void SetSurfaceType(Vector2I target, byte typeId);
        void SetTileState(Vector2I target, int state);
        void SetTileHazard(Vector2I target, byte hazardState);
        void BroadcastTileUpdate(Vector2I target, GridTileData data);

        void AddItemToGrid(Vector2I coord, string itemStringId);
        bool RemoveItemFromGrid(Vector2I coord, string itemStringId);
        void SpawnItemToGrid(Vector2I coord, string itemStringId);
    }
}
