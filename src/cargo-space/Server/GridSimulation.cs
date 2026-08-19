using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class GridSimulation : IJobExecutionContext
    {
        private Dictionary<Vector2I, GridTileData> _grid;
        private AStarGrid2D _pathfinding;
        private NetworkBridge _networkBridge;
        private JobManager _jobManager;
        private ZoneManager _zoneManager;

        // Entity-based pawn state
        private Dictionary<PawnId, Pawn> _pawns = new();
        private HashSet<IGridEntity> _dirtyEntities = new();
        private HashSet<Vector2I> _reservedTiles = new();

        // Flotsam collection state
        private float _flotsamProgress = 0f;

        // Physical items lying on the floor
        private Dictionary<Vector2I, List<string>> _groundItems = new();

        // Sparse machine entities for tiles with dynamic state
        private Dictionary<Vector2I, MachineEntity> _activeMachines = new();

        public GridSimulation(NetworkBridge networkBridge = null, ZoneManager zoneManager = null)
        {
            _networkBridge = networkBridge;
            _zoneManager = zoneManager;
            _grid = new Dictionary<Vector2I, GridTileData>();
            _jobManager = new JobManager(networkBridge);
            _pawns = new Dictionary<PawnId, Pawn>();
            _dirtyEntities = new HashSet<IGridEntity>();
            _reservedTiles = new HashSet<Vector2I>();

            InitializeGrid();
            InitializePathfinding();
        }

        private void InitializeGrid()
        {
            string[] blueprint = new string[]
            {
                ". . C . C . .",
                ". . D . D . .",
                ". . D . D . .",
                ". . D . D . .",
                ". . P . P . .",
                ". H C D C . .",
                ". . . . . . ."
            };

            // Map characters to the StringIds in tiles.json
            Dictionary<char, string> legend = new Dictionary<char, string>
            {
                { '.', "space" },
                { 'D', "deck" },
                { 'C', "console" },
                { 'H', "harpoon" }
            };

            int height = blueprint.Length;
            int width = blueprint[0].Replace(" ", "").Length; // ignore spaces for readability

            int offsetX = width / 2;
            int offsetY = height / 2;

            for (int row = 0; row < height; row++)
            {
                // Remove spaces so ". . C" becomes "..C"
                string cleanRow = blueprint[row].Replace(" ", "");

                for (int col = 0; col < width; col++)
                {
                    char c = cleanRow[col];
                    Vector2I coord = new Vector2I(col - offsetX, row - offsetY);

                    if (c == 'P')
                    {
                        // Spawn a Pawn, and put a Deck tile under them
                        _grid[coord] = new GridTileData(TileRegistry.GetId("deck"));

                        PawnId newPawnId = PawnId.Create();
                        Pawn newPawn = new Pawn(newPawnId, coord);
                        _pawns[newPawnId] = newPawn;
                        _dirtyEntities.Add(newPawn);
                    }
                    else if (legend.TryGetValue(c, out string stringId))
                    {
                        byte typeId = TileRegistry.GetId(stringId);
                        _grid[coord] = new GridTileData(typeId);

                        TileDefinition tileDef = TileRegistry.Get(typeId);
                        if (tileDef != null && (tileDef.HasTag("GeneratesPower") || tileDef.HasTag("ConsumesPower")))
                        {
                            RegisterMachine(coord);
                        }
                    }
                    else
                    {
                        GameLogger.Warning($"InitializeGrid: Unknown blueprint character '{c}' at {coord}. Defaulting to space.");
                        _grid[coord] = new GridTileData(TileRegistry.GetId("space"));
                    }
                }
            }
        }

        private void InitializePathfinding()
        {
            _pathfinding = new AStarGrid2D();
            _pathfinding.Region = new Rect2I(new Vector2I(-3, -3), new Vector2I(7, 7));
            _pathfinding.CellSize = new Vector2I(1, 1);
            _pathfinding.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
            _pathfinding.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan;
            _pathfinding.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
            
            _pathfinding.Update();
            
            // Set walkable/unwalkable tiles
            foreach (var kvp in _grid)
            {
                Vector2I coord = kvp.Key;
                TileDefinition tileDef = TileRegistry.Get(kvp.Value.TypeId);
                bool isWalkable = tileDef != null && tileDef.IsWalkable;
                _pathfinding.SetPointSolid(coord, !isWalkable);
            }
        }

        public Dictionary<Vector2I, GridTileData> GetGrid()
        {
            return new Dictionary<Vector2I, GridTileData>(_grid);
        }

        public IEnumerable<Pawn> GetPawns() => _pawns.Values;

        public void SpawnItemOnGrid(Vector2I coord, string itemStringId)
        {
            if (!_grid.ContainsKey(coord)) return; // Don't spawn in the void

            ItemDefinition def = ItemRegistry.Get(itemStringId);
            if (def == null)
            {
                GameLogger.Warning($"SpawnItemOnGrid: unknown item {itemStringId}");
                return;
            }

            Vector2I dropCoord = FindDropTile(coord, itemStringId, def.MaxStack) ?? coord;
            AddItemToGrid(dropCoord, itemStringId);
        }

        public void AddItemToGrid(Vector2I coord, string itemStringId)
        {
            if (!_grid.ContainsKey(coord)) return;

            if (!_groundItems.ContainsKey(coord))
            {
                _groundItems[coord] = new List<string>();
            }
            _groundItems[coord].Add(itemStringId);

            GameLogger.Debug($"Added {itemStringId} to {coord}. Tile now has {_groundItems[coord].Count} items.");

            _networkBridge?.BroadcastGroundItemsUpdate(coord, _groundItems[coord]);
        }

        public bool RemoveItemFromGrid(Vector2I coord, string itemStringId)
        {
            if (!_groundItems.TryGetValue(coord, out List<string> items))
            {
                GameLogger.Warning($"RemoveItemFromGrid: no items at {coord}");
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == itemStringId)
                {
                    items.RemoveAt(i);
                    if (items.Count == 0)
                    {
                        _groundItems.Remove(coord);
                    }

                    GameLogger.Debug($"Removed {itemStringId} from {coord}. Tile now has {items.Count} items.");
                    _networkBridge?.BroadcastGroundItemsUpdate(coord, _groundItems.TryGetValue(coord, out List<string> remaining) ? remaining : new List<string>());
                    return true;
                }
            }

            GameLogger.Warning($"RemoveItemFromGrid: {itemStringId} not found at {coord}");
            return false;
        }

        private Vector2I? FindDropTile(Vector2I start, string itemStringId, int maxStack)
        {
            Queue<Vector2I> queue = new();
            HashSet<Vector2I> visited = new();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                if (_grid.TryGetValue(current, out GridTileData tile))
                {
                    TileDefinition tileDef = TileRegistry.Get(tile.TypeId);
                    if (tileDef != null && tileDef.HasTag("StoresUnidentified") &&
                        !IsItemStackFull(current, itemStringId, maxStack))
                    {
                        return current;
                    }
                }

                foreach (Vector2I dir in new[] { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down })
                {
                    Vector2I next = current + dir;
                    if (!visited.Contains(next) && _grid.ContainsKey(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            GameLogger.Warning($"FindDropTile: no non-full walkable tile found near {start} for {itemStringId}");
            return null;
        }

        private bool IsItemStackFull(Vector2I coord, string itemStringId, int maxStack)
        {
            if (!_groundItems.TryGetValue(coord, out List<string> items))
                return false;

            int count = 0;
            foreach (string item in items)
            {
                if (item == itemStringId)
                {
                    count++;
                    if (count >= maxStack)
                        return true;
                }
            }

            return false;
        }

        public void AddJob(Job job)
        {
            // Hazard jobs can target any tile in the grid; SetState must be interactable.
            bool isValidTile = _grid.ContainsKey(job.Target);
            if (job.Type == JobType.SetState)
            {
                isValidTile = IsInteractableTile(job.Target);
            }
            else if (job.Type == JobType.Operate)
            {
                isValidTile = _grid.ContainsKey(job.Target) && TileRegistry.Get(_grid[job.Target].TypeId)?.HasTag("Operable") == true;
            }
            else if (job.Type == JobType.Haul)
            {
                isValidTile = IsValidHaulJob(job);
            }

            bool isActivelyWorked = _pawns.Values.Any(p => p.CurrentJob.Target == job.Target);
            _jobManager.AddJob(job, isValidTile, isActivelyWorked, job.OwnerPeerId);
        }

        private bool IsValidHaulJob(Job job)
        {
            if (!_grid.ContainsKey(job.Target) || !_grid.ContainsKey(job.Destination))
                return false;

            if (_groundItems == null || !_groundItems.TryGetValue(job.Target, out List<string> items) || !items.Contains(job.ItemId))
                return false;

            if (_zoneManager == null || !_zoneManager.IsStorageZone(job.Destination))
                return false;

            TileDefinition destDef = TileRegistry.Get(_grid[job.Destination].TypeId);
            if (destDef == null || !destDef.IsWalkable)
                return false;

            ItemDefinition itemDef = ItemRegistry.Get(job.ItemId);
            int maxStack = itemDef?.MaxStack ?? int.MaxValue;

            return !IsItemStackFull(job.Destination, job.ItemId, maxStack);
        }

        // IJobExecutionContext
        public bool IsInteractableTile(Vector2I target)
        {
            if (!_grid.ContainsKey(target)) return false;
            TileDefinition tileDef = TileRegistry.Get(_grid[target].TypeId);
            return tileDef != null && tileDef.IsInteractable;
        }

        public void SetTileState(Vector2I target, int state)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.State = state;
                _grid[target] = tileData;
                _networkBridge?.BroadcastTileUpdate(target, tileData);
            }
        }

        public void SetTileHazard(Vector2I target, byte hazardState)
        {
            if (_grid.ContainsKey(target))
            {
                GridTileData tileData = _grid[target];
                tileData.HazardState = hazardState;
                _grid[target] = tileData;
                _networkBridge?.BroadcastTileUpdate(target, tileData);
            }
        }

        public void BroadcastTileUpdate(Vector2I target, GridTileData data)
        {
            _networkBridge?.BroadcastTileUpdate(target, data);
        }

        public void Tick()
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.State == PawnState.Idle && _jobManager.BoardCount > 0)
                {
                    for (int i = 0; i < _jobManager.BoardCount; i++)
                    {
                        Job? potentialJob = _jobManager.ClaimNextAvailableJob(out int index);
                        if (potentialJob == null) break;

                        CalculatePath(pawn, potentialJob.Value.Target);

                        if (pawn.CurrentPath.Count > 0)
                        {
                            // Path found. Assign job.
                            pawn.CurrentJob = potentialJob.Value;
                            GameLogger.Debug($"Pawn {pawn.Id}: claimed job {pawn.CurrentJob.Id} at {pawn.CurrentJob.Target}");
                            break; // Stop looking for jobs
                        }
                        else
                        {
                            // Unreachable. Discard job and release reservation.
                            GameLogger.Warning($"Job {potentialJob.Value.Id} is unreachable. Discarding.");
                            _networkBridge?.BroadcastJobRemoved(potentialJob.Value.Id);
                            _jobManager.Release(potentialJob.Value.Target);
                        }
                    }
                }

                if ((pawn.State == PawnState.Walking || pawn.State == PawnState.Carrying) && pawn.CurrentPath.Count > 0)
                {
                    MoveAlongPath(pawn);
                }
                else if (pawn.State == PawnState.Working)
                {
                    WorkOnJob(pawn);
                }
                else if (pawn.State == PawnState.Operating)
                {
                    // Continuous operation: pawn remains locked on the tile.
                }
            }

            float currentRate = CalculateFlotsamRate();
            if (currentRate > 0)
            {
                _flotsamProgress += currentRate;
                if (_flotsamProgress >= 1.0f)
                {
                    _flotsamProgress -= 1.0f;
                    string caughtItem = "scrap_metal";

                    Pawn visualPawn = _pawns.Values.FirstOrDefault(p =>
                        p.State == PawnState.Operating &&
                        _grid.TryGetValue(p.CurrentJob.Target, out GridTileData t) &&
                        TileRegistry.Get(t.TypeId)?.Stats.TryGetValue("HarpoonRate", out float _) == true);

                    if (visualPawn == null) return;

                    Vector2I harpoonTarget = visualPawn.CurrentJob.Target;

                    // Find an adjacent walkable tile
                    Vector2I dropCoord = harpoonTarget;
                    Vector2I[] adjacents = { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down };
                    foreach (var dir in adjacents)
                    {
                        Vector2I test = harpoonTarget + dir;
                        if (_grid.TryGetValue(test, out GridTileData tile) && TileRegistry.Get(tile.TypeId)?.IsWalkable == true)
                        {
                            dropCoord = test;
                            break;
                        }
                    }

                    SpawnItemOnGrid(dropCoord, caughtItem);
                    _networkBridge?.BroadcastHarpoonCatch(harpoonTarget, caughtItem);
                }
            }

            GenerateHaulJobs();

            SimulatePowerGrid();
        }

        public void RegisterMachine(Vector2I coord)
        {
            _activeMachines[coord] = new MachineEntity(coord);
        }

        private void SimulatePowerGrid()
        {
            // 1. Cache the old state for diffing
            Dictionary<Vector2I, float> oldPowerStates = new Dictionary<Vector2I, float>();
            foreach (var kvp in _activeMachines)
            {
                oldPowerStates[kvp.Key] = kvp.Value.DynamicState.GetValueOrDefault("IsPowered", 0f);
                kvp.Value.DynamicState["IsPowered"] = 0f; // Reset for this tick
            }

            Queue<Vector2I> queue = new Queue<Vector2I>();
            HashSet<Vector2I> visited = new HashSet<Vector2I>();

            // 2. Find generators ONLY by searching the sparse active machines list
            foreach (var kvp in _activeMachines)
            {
                if (_grid.TryGetValue(kvp.Key, out GridTileData tileData))
                {
                    if (TileRegistry.Get(tileData.TypeId)?.HasTag("GeneratesPower") == true)
                    {
                        queue.Enqueue(kvp.Key);
                        visited.Add(kvp.Key);
                        kvp.Value.DynamicState["IsPowered"] = 1f;
                    }
                }
            }

            // 3. Flood fill
            while (queue.Count > 0)
            {
                Vector2I current = queue.Dequeue();

                foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I neighbor = current + dir;
                    if (visited.Contains(neighbor) || !_grid.TryGetValue(neighbor, out GridTileData neighborTile)) continue;

                    TileDefinition def = TileRegistry.Get(neighborTile.TypeId);
                    if (def != null && def.HasTag("TransfersPower"))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);

                        if (def.HasTag("ConsumesPower") && _activeMachines.TryGetValue(neighbor, out var machine))
                        {
                            machine.DynamicState["IsPowered"] = 1f;
                        }
                    }
                }
            }

            // 4. Delta Sync: Only broadcast changes!
            foreach (var kvp in _activeMachines)
            {
                float oldState = oldPowerStates[kvp.Key];
                float newState = kvp.Value.DynamicState["IsPowered"];

                if (oldState != newState)
                {
                    GameLogger.Debug($"Power State Changed at {kvp.Key}: {oldState} -> {newState}");
                    _networkBridge?.BroadcastMachineState(kvp.Key, "IsPowered", newState);
                }
            }
        }

        private float CalculateFlotsamRate()
        {
            float rate = 0f;
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.State == PawnState.Operating && pawn.CurrentJob.Type == JobType.Operate)
                {
                    if (_grid.TryGetValue(pawn.CurrentJob.Target, out GridTileData tile))
                    {
                        TileDefinition def = TileRegistry.Get(tile.TypeId);
                        if (def != null && def.Stats.TryGetValue("HarpoonRate", out float bonus))
                        {
                            rate += bonus;
                        }
                    }
                }
            }
            return rate;
        }

        private void CalculatePath(Pawn pawn, Vector2I target)
        {
            var godotPath = _pathfinding.GetIdPath(pawn.Position, target);
            pawn.CurrentPath = new List<Vector2I>(godotPath);
            if (pawn.CurrentPath.Count > 0)
            {
                pawn.State = PawnState.Walking;
                GameLogger.Debug($"Pawn {pawn.Id}: path calculated to {target}, {pawn.CurrentPath.Count} steps");
            }
        }

        private void MoveAlongPath(Pawn pawn)
        {
            if (pawn.CurrentPath.Count > 0)
            {
                Vector2I nextStep = pawn.CurrentPath[0];
                pawn.CurrentPath.RemoveAt(0);
                pawn.UpdatePosition(nextStep);
                _dirtyEntities.Add(pawn);

                GameLogger.Debug($"Pawn {pawn.Id}: moved to {nextStep}");

                // If path is complete, transition
                if (pawn.CurrentPath.Count == 0)
                {
                    if (pawn.CurrentJob.Type == JobType.Haul && pawn.State != PawnState.Carrying)
                    {
                        // Leg 1 complete: picked up the item, now walk to destination
                        GameLogger.Debug($"Pawn {pawn.Id}: picked up haul at {pawn.Position}, heading to {pawn.CurrentJob.Destination}");
                        pawn.State = PawnState.Carrying;
                        CalculatePath(pawn, pawn.CurrentJob.Destination);

                        if (pawn.CurrentPath.Count == 0)
                        {
                            GameLogger.Warning($"Pawn {pawn.Id}: no path to haul destination {pawn.CurrentJob.Destination}");
                            ResetPawnState(pawn);
                        }
                        else
                        {
                            pawn.State = PawnState.Carrying;
                        }
                    }
                    else
                    {
                        pawn.State = PawnState.Working;
                        pawn.WorkTicksRemaining = 3;
                        GameLogger.Debug($"Pawn {pawn.Id}: reached target, starting work");
                    }
                }
            }
        }

        private void ResetPawnState(Pawn pawn)
        {
            if (pawn.CurrentJob.Target != default)
            {
                _jobManager.Release(pawn.CurrentJob.Target);
            }
            if (pawn.CurrentJob.Type == JobType.Haul)
            {
                _jobManager.Release(pawn.CurrentJob.Destination);
            }
            pawn.CurrentPath.Clear();
            pawn.WorkTicksRemaining = 0;
            pawn.State = PawnState.Idle;
            pawn.CurrentJob = default;
        }

        private void WorkOnJob(Pawn pawn)
        {
            pawn.WorkTicksRemaining--;
            GameLogger.Debug($"Pawn {pawn.Id}: working... {pawn.WorkTicksRemaining} ticks remaining");

            if (pawn.WorkTicksRemaining <= 0)
            {
                _jobManager.ExecuteJob(pawn.CurrentJob, this);

                if (pawn.CurrentJob.Type == JobType.Operate)
                {
                    // Continuous Lock-in. Do NOT clear the job or release the reservation.
                    pawn.State = PawnState.Operating;
                    GameLogger.Debug($"Pawn {pawn.Id} locked into Operating state at {pawn.CurrentJob.Target}.");
                }
                else
                {
                    _networkBridge?.BroadcastJobRemoved(pawn.CurrentJob.Id);
                    pawn.CurrentJob = default;
                    pawn.State = PawnState.Idle;
                }
            }
        }

        private void CancelPawnOperation(Pawn pawn, JobId id, Vector2I target)
        {
            _networkBridge?.BroadcastJobRemoved(id);
            ResetPawnState(pawn);
            SetTileState(target, 0);
            GameLogger.Debug($"Pawn {pawn.Id}: operation cancelled at {target}");
        }

        public void CancelJob(JobId id)
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.CurrentJob.Id == id)
                {
                    if (pawn.CurrentJob.Type == JobType.Operate)
                    {
                        CancelPawnOperation(pawn, id, pawn.CurrentJob.Target);
                        return;
                    }

                    ResetPawnState(pawn);
                    _networkBridge?.BroadcastJobRemoved(id);
                    GameLogger.Debug($"Pawn {pawn.Id}: active job cancelled and interrupted: {id}");
                    return;
                }
            }

            _jobManager.CancelJob(id, _pawns.Values);
        }

        public void CancelOperationAt(Vector2I target)
        {
            foreach (Pawn pawn in _pawns.Values)
            {
                if (pawn.CurrentJob.Target == target && pawn.CurrentJob.Type == JobType.Operate)
                {
                    CancelPawnOperation(pawn, pawn.CurrentJob.Id, target);
                    return;
                }
            }

            GameLogger.Debug($"CancelOperationAt: no operating pawn found at {target}");
        }

        public bool TryMovePawn(Vector2I target)
        {
            // This method is deprecated - use job system instead
            return false;
        }

        public HashSet<IGridEntity> FlushDirtyEntities()
        {
            var copy = new HashSet<IGridEntity>(_dirtyEntities);
            _dirtyEntities.Clear();
            return copy;
        }

        private Vector2I? FindStorageDestination(string itemStringId)
        {
            if (_zoneManager == null || _zoneManager.ZoneTiles.Count == 0)
                return null;

            ItemDefinition itemDef = ItemRegistry.Get(itemStringId);
            int maxStack = itemDef?.MaxStack ?? int.MaxValue;

            foreach (Vector2I tile in _zoneManager.ZoneTiles.Keys)
            {
                if (!_grid.TryGetValue(tile, out GridTileData gridTile))
                    continue;

                TileDefinition tileDef = TileRegistry.Get(gridTile.TypeId);
                if (tileDef == null || !tileDef.IsWalkable)
                    continue;

                if (_jobManager.IsReserved(tile) || _jobManager.HasPendingHaulDestination(tile))
                    continue;

                if (!IsItemStackFull(tile, itemStringId, maxStack))
                {
                    return tile;
                }
            }

            return null;
        }

        private void GenerateHaulJobs()
        {
            if (_zoneManager == null || _zoneManager.ZoneTiles.Count == 0)
                return;

            foreach (var kvp in _groundItems)
            {
                Vector2I itemCoord = kvp.Key;

                if (_zoneManager.IsStorageZone(itemCoord))
                    continue;

                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                // Skip if a pawn is already working this tile or if a job is reserved
                if (_pawns.Values.Any(p => p.CurrentJob.Target == itemCoord))
                    continue;
                if (_jobManager.IsReserved(itemCoord) || _jobManager.HasPendingJobForTarget(itemCoord))
                    continue;

                // Generate one haul job for the first item type on this tile
                string itemId = kvp.Value[0];
                Vector2I? destination = FindStorageDestination(itemId);

                if (destination == null)
                {
                    GameLogger.Debug($"GenerateHaulJobs: no available storage destination for {itemId}");
                    continue;
                }

                JobId newJobId = JobId.Create();
                Job haulJob = new Job(newJobId, 0, itemCoord, destination.Value, JobType.Haul, 0, itemId);
                AddJob(haulJob);
                GameLogger.Debug($"GenerateHaulJobs: created haul job {newJobId} for {itemId} from {itemCoord} to {destination.Value}");
            }
        }
    }
}
