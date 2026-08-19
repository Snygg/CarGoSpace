using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public class ClientDataCache
    {
        private Dictionary<Vector2I, GridTileData> _grid = new();
        private List<Job> _activeJobs = new();
        private Dictionary<Vector2I, List<string>> _groundItems = new();
        private Dictionary<Vector2I, ZoneType> _zoneTiles = new();
        private HashSet<Vector2I> _activeHazards = new();

        private int _expectedTileCount = 0;
        private bool _gridRendered = false;

        public int ExpectedTileCount
        {
            get => _expectedTileCount;
            set => _expectedTileCount = value;
        }

        public bool IsGridRendered
        {
            get => _gridRendered;
            set => _gridRendered = value;
        }

        // Grid
        public Dictionary<Vector2I, GridTileData> Grid => _grid;

        public IReadOnlyCollection<Vector2I> ActiveHazards => _activeHazards;

        public void ClearGrid()
        {
            _grid.Clear();
            _activeHazards.Clear();
        }

        public void UpdateTile(Vector2I coord, GridTileData data)
        {
            _grid[coord] = data;
            if (data.HazardState > 0)
            {
                _activeHazards.Add(coord);
            }
            else
            {
                _activeHazards.Remove(coord);
            }
        }

        public bool TryGetTile(Vector2I coord, out GridTileData data) => _grid.TryGetValue(coord, out data);

        public GridTileData GetTile(Vector2I coord) => _grid.TryGetValue(coord, out GridTileData data) ? data : default;

        public IReadOnlyDictionary<Vector2I, GridTileData> GetGrid() => _grid;

        // Ground items
        public void UpdateGroundItems(Vector2I coord, List<string> items) => _groundItems[coord] = items;

        public bool TryGetGroundItems(Vector2I coord, out List<string> items) => _groundItems.TryGetValue(coord, out items);

        public IReadOnlyDictionary<Vector2I, List<string>> GetAllGroundItems() => _groundItems;

        // Zones
        public void UpdateZone(Vector2I coord, ZoneType type) => _zoneTiles[coord] = type;

        public void ClearZones() => _zoneTiles.Clear();

        public void SetZones(Dictionary<Vector2I, ZoneType> zones)
        {
            _zoneTiles = new Dictionary<Vector2I, ZoneType>(zones);
        }

        public IReadOnlyDictionary<Vector2I, ZoneType> GetAllZones() => _zoneTiles;

        public ZoneType GetZone(Vector2I coord) => _zoneTiles.TryGetValue(coord, out ZoneType type) ? type : ZoneType.None;

        // Jobs
        public void AddJob(Job job)
        {
            if (TryFindJob(job.Id, out _))
            {
                return;
            }

            _activeJobs.Add(job);
        }

        public bool RemoveJob(JobId id)
        {
            for (int i = 0; i < _activeJobs.Count; i++)
            {
                if (_activeJobs[i].Id == id)
                {
                    _activeJobs.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool TryFindJob(JobId id, out Job job)
        {
            foreach (Job j in _activeJobs)
            {
                if (j.Id == id)
                {
                    job = j;
                    return true;
                }
            }

            job = default;
            return false;
        }

        public IReadOnlyList<Job> GetActiveJobs() => _activeJobs;
    }
}
