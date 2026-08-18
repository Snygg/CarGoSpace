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
        private NetworkBridge _networkBridge;

        public ServerManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            GD.Print("[Server] ServerManager._Ready() called");
            _gridSimulation = new GridSimulation();
            StartServer();
        }

        private void StartServer()
        {
            GD.Print("[Server] Starting server...");
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateServer(Constants.ServerPort, 32);
            
            if (error != Error.Ok)
            {
                GD.PrintErr($"[Server] Failed to start server: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            
            GD.Print($"[Server] Server started successfully on port {Constants.ServerPort}");
        }

        private void OnPeerConnected(long id)
        {
            GD.Print($"[Server] Client connected: {id}");
        }

        private void OnPeerDisconnected(long id)
        {
            GD.Print($"[Server] Client disconnected: {id}");
        }

        // Handler methods called by NetworkBridge
        public void HandleGridRequest(long requesterId)
        {
            GD.Print($"[Server] HandleGridRequest: Sending grid to client {requesterId}");
            SendGridToClient(requesterId);
            SendPawnPositionToClient(requesterId);
        }

        public void HandleMoveCommand(Vector2I target, long senderId)
        {
            GD.Print($"[Server] HandleMoveCommand: Move from {senderId} to {target}");
            
            if (_gridSimulation.TryMovePawn(target))
            {
                GD.Print($"[Server] Pawn moved to {target}");
                BroadcastPawnPosition();
            }
            else
            {
                GD.Print($"[Server] Invalid move to {target}");
            }
        }

        public async void SendGridToClient(long clientId)
        {
            var grid = _gridSimulation.GetGrid();
            GD.Print($"[Server] SendGridToClient: Sending {grid.Count} tiles to client {clientId}");
            
            // Send grid size first via NetworkBridge
            _networkBridge.SendGridSize(clientId, grid.Count);
            GD.Print($"[Server] Sent grid size: {grid.Count}");
            
            // Send each tile individually with a small delay to prevent network overload
            int tileCount = 0;
            foreach (var kvp in grid)
            {
                _networkBridge.SendTile(clientId, kvp.Key.X, kvp.Key.Y, (byte)kvp.Value.Type);
                tileCount++;
                
                // Add a small delay every 10 tiles to prevent network congestion
                if (tileCount % 10 == 0)
                {
                    await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
                }
            }
            GD.Print($"[Server] Sent {tileCount} individual tile RPCs");
            
            // Small delay before sending completion signal to ensure all tiles arrive
            await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
            
            // Signal that grid transfer is complete via NetworkBridge
            _networkBridge.SendGridComplete(clientId);
            GD.Print($"[Server] Sent grid complete signal");
        }

        public void SendPawnPositionToClient(long clientId)
        {
            var pawnPos = _gridSimulation.GetPawnPosition();
            GD.Print($"[Server] SendPawnPositionToClient: Sending pawn position {pawnPos} to client {clientId}");
            _networkBridge.SendPawnPosition(clientId, pawnPos);
        }

        private void BroadcastPawnPosition()
        {
            var pawnPos = _gridSimulation.GetPawnPosition();
            GD.Print($"[Server] BroadcastPawnPosition: Broadcasting pawn position {pawnPos} to all clients");
            _networkBridge.BroadcastPawnPosition(pawnPos);
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
