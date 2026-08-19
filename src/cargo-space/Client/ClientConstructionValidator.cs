using Godot;
using CargoSpace.Core;

namespace CargoSpace.Client
{
    public class ClientConstructionValidator
    {
        private ClientDataCache _dataCache;

        public ClientConstructionValidator(ClientDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public bool CanPlaceBlueprint(Vector2I coord, TileDefinition targetDef)
        {
            if (_dataCache == null || targetDef == null)
                return false;

            if (_dataCache.Blueprints.ContainsKey(coord))
                return false;

            if (!_dataCache.TryGetTile(coord, out GridTileData currentTile))
                return false;

            if (targetDef.Layer == "Floor")
            {
                if (currentTile.SurfaceTypeId != 0)
                    return false;

                TileDefinition currentDef = TileRegistry.Get(currentTile.TypeId);
                if (currentDef == null)
                    return false;

                if (currentDef.Layer == "Floor")
                    return true;

                if (currentDef.Layer == "Base")
                    return IsAdjacentToShip(coord);

                return false;
            }
            else if (targetDef.Layer == "Surface")
            {
                TileDefinition currentDef = currentTile.GetEffectiveDefinition();
                if (currentDef == null || currentTile.SurfaceTypeId != 0)
                    return false;

                // Surfaces can be built on a floor, or on a space tile that
                // touches the ship so you can place a hull/wall around the edge.
                if (currentDef.Layer == "Floor")
                    return true;

                if (currentDef.Layer == "Base")
                    return IsAdjacentToShip(coord);

                return false;
            }

            return false;
        }

        private bool IsAdjacentToShip(Vector2I coord)
        {
            foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
            {
                Vector2I neighbor = coord + dir;
                if (_dataCache.TryGetTile(neighbor, out GridTileData tile))
                {
                    TileDefinition def = tile.GetEffectiveDefinition();
                    if (def != null && (def.Layer == "Floor" || def.Layer == "Surface"))
                        return true;
                }
            }
            return false;
        }
    }
}
