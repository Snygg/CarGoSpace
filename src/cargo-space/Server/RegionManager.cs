using Godot;
using CargoSpace.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class RegionEntity
    {
        public Guid Id { get; }
        public HashSet<Vector2I> Tiles { get; }
        public bool TouchesSpace { get; set; }
        public int Version { get; set; }

        public RegionEntity(Guid id, IEnumerable<Vector2I> tiles)
        {
            Id = id;
            Tiles = new HashSet<Vector2I>(tiles);
        }
    }

    public class RegionDelta
    {
        public List<RegionEntity> Created { get; } = new List<RegionEntity>();
        public List<Guid> Destroyed { get; } = new List<Guid>();
        public List<(Guid Surviving, Guid Consumed)> Merged { get; } = new List<(Guid, Guid)>();
        public List<Guid> Split { get; } = new List<Guid>();
        public List<Guid> Survived { get; } = new List<Guid>();
    }

    public class RegionManager
    {
        private Dictionary<Vector2I, Guid> _tileToRegion = new();
        private Dictionary<Guid, RegionEntity> _regions = new();
        private int _version = 0;

        public RegionDelta LastDelta { get; private set; }

        public RegionDelta Recalculate(Dictionary<Vector2I, GridTileData> grid)
        {
            _version++;

            // 1. Snapshot old topology.
            var oldRegions = _regions.ToDictionary(
                kvp => kvp.Key,
                kvp => new RegionEntity(kvp.Value.Id, kvp.Value.Tiles)
                {
                    TouchesSpace = kvp.Value.TouchesSpace,
                    Version = kvp.Value.Version
                });
            var oldTileToRegion = new Dictionary<Vector2I, Guid>(_tileToRegion);

            _tileToRegion.Clear();
            _regions.Clear();

            // 2. Flood fill the new topology into temporary components.
            var newComponents = new List<HashSet<Vector2I>>();
            var touchesSpace = new List<bool>();
            var tempTileToIndex = new Dictionary<Vector2I, int>();

            foreach (var kvp in grid)
            {
                Vector2I coord = kvp.Key;
                GridTileData tile = kvp.Value;

                if (!IsWalkable(tile) || tempTileToIndex.ContainsKey(coord))
                    continue;

                var component = new HashSet<Vector2I>();
                bool touches = false;
                FloodFill(grid, coord, newComponents.Count, component, tempTileToIndex, ref touches);
                newComponents.Add(component);
                touchesSpace.Add(touches);
            }

            // 3. Build overlap scores and the lexicographically smallest shared tile.
            var overlap = new Dictionary<int, Dictionary<Guid, int>>();
            var minSharedTile = new Dictionary<int, Dictionary<Guid, Vector2I?>>();

            for (int i = 0; i < newComponents.Count; i++)
            {
                overlap[i] = new Dictionary<Guid, int>();
                minSharedTile[i] = new Dictionary<Guid, Vector2I?>();
                foreach (Vector2I tile in newComponents[i])
                {
                    if (oldTileToRegion.TryGetValue(tile, out var oldGuid))
                    {
                        overlap[i][oldGuid] = overlap[i].GetValueOrDefault(oldGuid, 0) + 1;

                        if (!minSharedTile[i].TryGetValue(oldGuid, out var minTile) ||
                            minTile == null ||
                            tile.X < minTile.Value.X ||
                            (tile.X == minTile.Value.X && tile.Y < minTile.Value.Y))
                        {
                            minSharedTile[i][oldGuid] = tile;
                        }
                    }
                }
            }

            // 4. Build sorted candidate assignments: overlap > old size > spatial tie-break.
            var candidates = new List<(int newIndex, Guid oldGuid, int overlap, int oldSize, Vector2I? minTile)>();
            for (int i = 0; i < newComponents.Count; i++)
            {
                foreach (var (oldGuid, count) in overlap[i])
                {
                    int oldSize = oldRegions[oldGuid].Tiles.Count;
                    candidates.Add((i, oldGuid, count, oldSize, minSharedTile[i].GetValueOrDefault(oldGuid, null)));
                }
            }

            candidates.Sort((a, b) =>
            {
                int cmp = b.overlap.CompareTo(a.overlap);       // larger overlap first
                if (cmp != 0) return cmp;
                cmp = b.oldSize.CompareTo(a.oldSize);           // larger original room next
                if (cmp != 0) return cmp;

                // spatial tie-break: lower X, then lower Y
                Vector2I ta = a.minTile ?? new Vector2I(int.MaxValue, int.MaxValue);
                Vector2I tb = b.minTile ?? new Vector2I(int.MaxValue, int.MaxValue);
                if (ta.X != tb.X) return ta.X.CompareTo(tb.X);
                return ta.Y.CompareTo(tb.Y);
            });

            // 5. Greedy one-to-one matching.
            var newIndexToGuid = new Dictionary<int, Guid>();
            var assignedOldGuids = new HashSet<Guid>();
            foreach (var c in candidates)
            {
                if (newIndexToGuid.ContainsKey(c.newIndex))
                    continue;
                if (assignedOldGuids.Contains(c.oldGuid))
                    continue;

                newIndexToGuid[c.newIndex] = c.oldGuid;
                assignedOldGuids.Add(c.oldGuid);
            }

            for (int i = 0; i < newComponents.Count; i++)
            {
                if (!newIndexToGuid.ContainsKey(i))
                    newIndexToGuid[i] = Guid.NewGuid();
            }

            // 6. Build the new authoritative maps.
            for (int i = 0; i < newComponents.Count; i++)
            {
                Guid guid = newIndexToGuid[i];
                var entity = new RegionEntity(guid, newComponents[i])
                {
                    TouchesSpace = touchesSpace[i],
                    Version = _version
                };
                _regions[guid] = entity;

                foreach (Vector2I tile in newComponents[i])
                    _tileToRegion[tile] = guid;
            }

            // 7. Build the RegionDelta.
            var delta = new RegionDelta();
            var consumedOldGuids = new HashSet<Guid>();

            var oldGuidToNewIndex = new Dictionary<Guid, int>();
            foreach (var kvp in newIndexToGuid)
                oldGuidToNewIndex[kvp.Value] = kvp.Key;

            var newToOldOverlaps = new List<HashSet<Guid>>();
            for (int i = 0; i < newComponents.Count; i++)
                newToOldOverlaps.Add(new HashSet<Guid>(overlap[i].Keys));

            // Assigned old regions: survive, split, or merge.
            foreach (var kvp in newIndexToGuid)
            {
                int newIndex = kvp.Key;
                Guid oldGuid = kvp.Value;

                if (!oldRegions.ContainsKey(oldGuid))
                    continue; // newly assigned - no prior history

                int overlapCount = overlap[newIndex].GetValueOrDefault(oldGuid, 0);
                int oldSize = oldRegions[oldGuid].Tiles.Count;

                if (overlapCount < oldSize)
                {
                    delta.Split.Add(oldGuid);
                }
                else
                {
                    bool merged = false;
                    foreach (Guid otherOld in newToOldOverlaps[newIndex])
                    {
                        if (otherOld == oldGuid)
                            continue;

                        if (oldGuidToNewIndex.ContainsKey(otherOld))
                            continue; // other half is a split fragment, not consumed

                        delta.Merged.Add((oldGuid, otherOld));
                        consumedOldGuids.Add(otherOld);
                        merged = true;
                    }

                    if (merged)
                        delta.Survived.Add(oldGuid); // survivor of a merge
                    else
                        delta.Survived.Add(oldGuid);
                }
            }

            // Old regions that were not assigned (consumed or destroyed).
            foreach (Guid oldGuid in oldRegions.Keys)
            {
                if (oldGuidToNewIndex.ContainsKey(oldGuid) || consumedOldGuids.Contains(oldGuid))
                    continue;

                delta.Destroyed.Add(oldGuid);
            }

            // New regions that are truly new.
            for (int i = 0; i < newComponents.Count; i++)
            {
                if (!oldRegions.ContainsKey(newIndexToGuid[i]))
                    delta.Created.Add(_regions[newIndexToGuid[i]]);
            }

            LastDelta = delta;
            return delta;
        }

        private void FloodFill(Dictionary<Vector2I, GridTileData> grid, Vector2I start, int componentIndex, HashSet<Vector2I> component, Dictionary<Vector2I, int> tempTileToIndex, ref bool touchesSpace)
        {
            Queue<Vector2I> queue = new();
            queue.Enqueue(start);
            tempTileToIndex[start] = componentIndex;
            component.Add(start);

            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I next = current + dir;

                    if (!grid.TryGetValue(next, out GridTileData tile) ||
                        tile.GetEffectiveDefinition()?.HasTag("Vacuum") == true)
                    {
                        touchesSpace = true;
                        continue;
                    }

                    if (IsWalkable(tile) && !tempTileToIndex.ContainsKey(next))
                    {
                        tempTileToIndex[next] = componentIndex;
                        component.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
        }

        private bool IsWalkable(GridTileData tile)
        {
            return tile.GetEffectiveDefinition()?.IsWalkable == true;
        }

        public bool TryGetRegion(Vector2I coord, out Guid regionId)
        {
            return _tileToRegion.TryGetValue(coord, out regionId);
        }

        public bool IsReachable(Vector2I a, Vector2I b)
        {
            return _tileToRegion.TryGetValue(a, out var regionA) &&
                   _tileToRegion.TryGetValue(b, out var regionB) &&
                   regionA == regionB;
        }

        public bool IsRegionExposedToSpace(Guid regionId)
        {
            return _regions.TryGetValue(regionId, out var region) && region.TouchesSpace;
        }

        public IEnumerable<Guid> GetAllRegions()
        {
            return _regions.Keys;
        }

        public IReadOnlyCollection<Vector2I> GetTilesInRegion(Guid regionId)
        {
            if (_regions.TryGetValue(regionId, out var region))
                return region.Tiles;
            return new List<Vector2I>();
        }

        public bool TryGetRepresentativeTile(Guid regionId, out Vector2I tile)
        {
            if (_regions.TryGetValue(regionId, out var region) && region.Tiles.Count > 0)
            {
                tile = region.Tiles.OrderBy(t => t.X).ThenBy(t => t.Y).First();
                return true;
            }

            tile = default;
            return false;
        }

    }
}
