using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;

namespace CargoSpace.Server
{
    public class PowerManager
    {
        private GridSimulation _gridSimulation;
        private NetworkBridge _networkBridge;

        private Dictionary<Vector2I, MachineEntity> _activeMachines = new();

        public PowerManager(GridSimulation gridSimulation, NetworkBridge networkBridge)
        {
            _gridSimulation = gridSimulation;
            _networkBridge = networkBridge;
        }

        public void RegisterMachine(Vector2I coord)
        {
            _activeMachines[coord] = new MachineEntity(coord);
        }

        public void UnregisterMachine(Vector2I coord)
        {
            _activeMachines.Remove(coord);
        }

        public void OnTileChanged(Vector2I coord)
        {
            if (!_gridSimulation.TryGetTile(coord, out GridTileData tileData))
                return;

            TileDefinition def = tileData.GetEffectiveDefinition();
            bool isMachine = def != null && (def.HasTag("GeneratesPower") || def.HasTag("ConsumesPower"));

            if (isMachine && !_activeMachines.ContainsKey(coord))
                RegisterMachine(coord);
            else if (!isMachine && _activeMachines.ContainsKey(coord))
                UnregisterMachine(coord);
        }

        public void Tick()
        {
            SimulatePowerGrid();
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
                if (_gridSimulation.TryGetTile(kvp.Key, out GridTileData tileData))
                {
                    if (tileData.GetEffectiveDefinition()?.HasTag("GeneratesPower") == true)
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
                    if (visited.Contains(neighbor) || !_gridSimulation.TryGetTile(neighbor, out GridTileData neighborTile))
                        continue;

                    TileDefinition def = neighborTile.GetEffectiveDefinition();
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
    }
}
