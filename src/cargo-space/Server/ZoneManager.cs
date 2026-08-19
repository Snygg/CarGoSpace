using Godot;
using System.Collections.Generic;
using System.Linq;
using CargoSpace.Core;
using CargoSpace.Shared;

namespace CargoSpace.Server
{
    public class ZoneManager
    {
        public Dictionary<Vector2I, ZoneType> ZoneTiles = new();

        private NetworkBridge _networkBridge;

        public ZoneManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public void ToggleZoneTiles(Vector2I[] tiles, byte zoneTypeByte)
        {
            ZoneType type = (ZoneType)zoneTypeByte;
            foreach (Vector2I tile in tiles)
            {
                if (type == ZoneType.None)
                {
                    ZoneTiles.Remove(tile);
                }
                else
                {
                    ZoneTiles[tile] = type;
                }
            }

            GameLogger.Debug($"ZoneManager: toggled {tiles.Length} tiles (type={type}). ZoneTiles now has {ZoneTiles.Count} tiles.");

            _networkBridge?.BroadcastZoneUpdate(ZoneTiles);
        }

        public bool IsStorageZone(Vector2I tile)
        {
            return ZoneTiles.TryGetValue(tile, out ZoneType type) && type == ZoneType.Storage;
        }
    }
}
