using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public class ClientRegionManager
    {
        private Dictionary<Vector2I, int> _regionByCoord = new();
        private int _nextRegionId = 1;

        public void Recalculate(Dictionary<Vector2I, GridTileData> grid)
        {
            _regionByCoord.Clear();
            _nextRegionId = 1;

            foreach (var kvp in grid)
            {
                Vector2I coord = kvp.Key;
                GridTileData tile = kvp.Value;

                if (!IsWalkable(tile) || _regionByCoord.ContainsKey(coord))
                    continue;

                FloodFill(grid, coord, _nextRegionId++);
            }
        }

        private void FloodFill(Dictionary<Vector2I, GridTileData> grid, Vector2I start, int regionId)
        {
            Queue<Vector2I> queue = new();
            queue.Enqueue(start);
            _regionByCoord[start] = regionId;

            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I next = current + dir;

                    if (grid.TryGetValue(next, out GridTileData tile) &&
                        IsWalkable(tile) &&
                        !_regionByCoord.ContainsKey(next))
                    {
                        _regionByCoord[next] = regionId;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        private bool IsWalkable(GridTileData tile)
        {
            return tile.GetEffectiveDefinition()?.IsWalkable == true;
        }

        public bool TryGetRegion(Vector2I coord, out int regionId)
        {
            return _regionByCoord.TryGetValue(coord, out regionId);
        }

        public bool IsReachable(Vector2I a, Vector2I b)
        {
            return _regionByCoord.TryGetValue(a, out int regionA) &&
                   _regionByCoord.TryGetValue(b, out int regionB) &&
                   regionA == regionB;
        }

        public IEnumerable<int> GetAllRegions()
        {
            HashSet<int> regions = new HashSet<int>();
            foreach (int id in _regionByCoord.Values)
            {
                regions.Add(id);
            }

            return regions;
        }

        public bool TryGetRepresentativeTile(int regionId, out Vector2I tile)
        {
            foreach (var kvp in _regionByCoord)
            {
                if (kvp.Value == regionId)
                {
                    tile = kvp.Key;
                    return true;
                }
            }

            tile = default;
            return false;
        }

        public IEnumerable<Vector2I> GetTilesInRegion(int regionId)
        {
            foreach (var kvp in _regionByCoord)
            {
                if (kvp.Value == regionId)
                    yield return kvp.Key;
            }
        }
    }
}
