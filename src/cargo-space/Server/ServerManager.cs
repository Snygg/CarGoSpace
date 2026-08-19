using Godot;
using CargoSpace.Core;
using System.Collections.Generic;
using CargoSpace.Shared;

namespace CargoSpace.Server
{
    public partial class ServerManager : Node
    {
        private ENetMultiplayerPeer _peer;
        private GridSimulation _gridSimulation;
        private ZoneManager _zoneManager;
        private NetworkBridge _networkBridge;
        private Timer _tickTimer;

        public ServerManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            GameLogger.Debug("ServerManager._Ready() called");
            _zoneManager = new ZoneManager(_networkBridge);
            _gridSimulation = new GridSimulation(_networkBridge, _zoneManager);
            StartServer();
            StartTickLoop();
        }

        private void StartServer()
        {
            GameLogger.Debug("Starting server...");
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateServer(Constants.ServerPort, 32);
            
            if (error != Error.Ok)
            {
                GameLogger.Error($"Failed to start server: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            
            GameLogger.Debug($"Server started successfully on port {Constants.ServerPort}");
        }

        private void StartTickLoop()
        {
            _tickTimer = new Timer();
            _tickTimer.WaitTime = 0.5; // Tick every 0.5 seconds
            _tickTimer.Autostart = true;
            _tickTimer.Timeout += OnTick;
            AddChild(_tickTimer);
            GameLogger.Debug("Server tick loop started (0.5s interval)");
        }

        private void OnTick()
        {
            _gridSimulation.Tick();
            
            // Flush and broadcast any dirty entities
            foreach (IGridEntity entity in _gridSimulation.FlushDirtyEntities())
            {
                _networkBridge.BroadcastEntityPosition(entity);
                entity.ClearDirtyFlag();
            }
        }

        private void OnPeerConnected(long id)
        {
            GameLogger.Debug($"Client connected: {id}");
        }

        private void OnPeerDisconnected(long id)
        {
            GameLogger.Debug($"Client disconnected: {id}");
        }

        // Handler methods called by NetworkBridge
        public void HandleGridRequest(long requesterId)
        {
            GameLogger.Debug($"HandleGridRequest: Sending grid to client {requesterId}");
            SendGridToClient(requesterId);
            SendPawnPositionToClient(requesterId);
        }

        public void HandleJobCommand(JobId id, Vector2I target, JobType jobType, int targetState, long senderId)
        {
            GameLogger.Debug($"HandleJobCommand: Job request from {senderId} to {target}, type {jobType}, state {targetState}, id {id}");

            // Add job to the simulation's job board
            _gridSimulation.AddJob(new Job(id, senderId, target, jobType, targetState));
        }

        public void HandleCancelJobRequest(JobId id)
        {
            GameLogger.Debug($"HandleCancelJobRequest: Cancelling job {id}");
            _gridSimulation.CancelJob(id);
        }

        public void HandleCancelOperationAt(Vector2I target)
        {
            GameLogger.Debug($"HandleCancelOperationAt: Stopping operation at {target}");
            _gridSimulation.CancelOperationAt(target);
        }

        public void HandlePlaceBlueprint(Vector2I coord, byte targetTypeId, long senderId)
        {
            GameLogger.Debug($"HandlePlaceBlueprint: {senderId} wants type {targetTypeId} at {coord}");
            _gridSimulation.PlaceBlueprint(coord, targetTypeId);
        }

        public void HandleToggleZoneTiles(Vector2I[] tiles, byte zoneType)
        {
            GameLogger.Debug($"HandleToggleZoneTiles: {tiles.Length} tiles (zoneType={zoneType})");
            _zoneManager?.ToggleZoneTiles(tiles, zoneType);
        }

        public async void SendGridToClient(long clientId)
        {
            var grid = _gridSimulation.GetGrid();
            GameLogger.Debug($"SendGridToClient: Sending {grid.Count} tiles to client {clientId}");
            
            // Send grid size first via NetworkBridge
            _networkBridge.SendGridSize(clientId, grid.Count);
            
            // Send each tile individually with a small delay to prevent network overload
            int tileCount = 0;
            foreach (var kvp in grid)
            {
                _networkBridge.SendTile(clientId, kvp.Key.X, kvp.Key.Y, kvp.Value.TypeId, kvp.Value.State, kvp.Value.HazardState, kvp.Value.SurfaceTypeId);
                tileCount++;
                
                // Add a small delay every 10 tiles to prevent network congestion
                if (tileCount % 10 == 0)
                {
                    await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
                }
            }
            
            GameLogger.Debug($"Sent {tileCount} individual tile RPCs");
            
            // Send any existing construction blueprints before completion
            foreach (Blueprint bp in _gridSimulation.GetBlueprints())
            {
                _networkBridge.SendBlueprintState(clientId, bp);
            }

            // Small delay before sending completion signal to ensure all tiles arrive
            await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
            
            // Signal that grid transfer is complete via NetworkBridge
            _networkBridge.SendGridComplete(clientId);
        }

        public void SendPawnPositionToClient(long clientId)
        {
            foreach (Pawn pawn in _gridSimulation.GetPawns())
            {
                GameLogger.Debug($"SendPawnPositionToClient: Sending pawn position {pawn.Position} to client {clientId}");
                _networkBridge.SendEntityPosition(clientId, pawn);
            }
        }

        public override void _ExitTree()
        {
            if (_peer != null)
            {
                _peer.Close();
            }
        }
    }
}
