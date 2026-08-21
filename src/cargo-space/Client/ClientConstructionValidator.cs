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

            GridTileData currentTile = _dataCache.GetTile(coord);
            TileDefinition currentDef = currentTile.GetEffectiveDefinition();
            if (currentDef == null)
                return false;

            if (targetDef.Layer == LayerType.Floor)
            {
                // Floors replace the base tile and must not be placed under an existing surface
                if (currentTile.SurfaceTypeId != 0)
                    return false;

                // No reason to build the same floor on top of itself
                if (currentTile.TypeId == targetDef.TypeId)
                    return false;

                if (currentDef.HasTag("Vacuum"))
                    return IsAdjacentToShip(coord);

                if (currentDef.Layer == LayerType.Floor)
                    return true;

                return false;
            }
            else if (targetDef.Layer == LayerType.Surface)
            {
                // Surfaces sit on top of a floor. They cannot be placed on another
                // surface, nor can they be placed directly on space/vacuum.
                if (currentTile.SurfaceTypeId != 0)
                    return false;

                if (currentDef.HasTag("Vacuum"))
                    return false;

                if (currentDef.Layer == LayerType.Floor)
                    return true;

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
                    if (def != null && !def.HasTag("Vacuum") &&
                        (def.Layer == LayerType.Floor || def.Layer == LayerType.Surface))
                        return true;
                }
            }
            return false;
        }
    }
}
