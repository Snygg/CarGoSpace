using Godot;
using System.Collections.Generic;
using System.Linq;
using CargoSpace.Core;
using CargoSpace.Shared;

namespace CargoSpace.Server
{
    public class ZoneManager
    {
        public HashSet<Vector2I> StorageZoneTiles = new();

        private NetworkBridge _networkBridge;

        public ZoneManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public void ToggleZoneTiles(Vector2I[] tiles, bool isAdding)
        {
            foreach (Vector2I tile in tiles)
            {
                if (isAdding)
                {
                    StorageZoneTiles.Add(tile);
                }
                else
                {
                    StorageZoneTiles.Remove(tile);
                }
            }

            GameLogger.Debug($"ZoneManager: toggled {tiles.Length} tiles (adding={isAdding}). StorageZoneTiles now has {StorageZoneTiles.Count} tiles.");

            _networkBridge?.BroadcastZoneUpdate(StorageZoneTiles.ToArray());
        }

        public bool IsStorageZone(Vector2I tile)
        {
            return StorageZoneTiles.Contains(tile);
        }
    }
}
