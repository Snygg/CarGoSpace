using Godot;
using CargoSpace.Core;
using System.Collections.Generic;
using CargoSpace.Client;

namespace CargoSpace.Server
{
    public partial class ServerManager : Node
    {
        private ENetMultiplayerPeer _peer;
        private GridSimulation _gridSimulation;

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
            SendGridToClient(id);
            SendPawnPositionToClient(id);
        }

        private void OnPeerDisconnected(long id)
        {
            GD.Print($"[Server] Client disconnected: {id}");
        }

        private void SendGridToClient(long clientId)
        {
            var grid = _gridSimulation.GetGrid();
            GD.Print($"[Server] SendGridToClient: Sending {grid.Count} tiles to client {clientId}");
            
            // Send grid size first
            RpcId(clientId, nameof(ClientManager.ReceiveGridSize), grid.Count);
            GD.Print($"[Server] Sent grid size: {grid.Count}");
            
            // Send each tile individually
            int tileCount = 0;
            foreach (var kvp in grid)
            {
                RpcId(clientId, nameof(ClientManager.ReceiveTile), kvp.Key.X, kvp.Key.Y, (byte)kvp.Value.Type);
                tileCount++;
            }
            GD.Print($"[Server] Sent {tileCount} individual tile RPCs");
            
            // Signal that grid transfer is complete
            RpcId(clientId, nameof(ClientManager.ReceiveGridComplete));
            GD.Print($"[Server] Sent grid complete signal");
        }

        private void SendPawnPositionToClient(long clientId)
        {
            var pawnPos = _gridSimulation.GetPawnPosition();
            GD.Print($"[Server] SendPawnPositionToClient: Sending pawn position {pawnPos} to client {clientId}");
            RpcId(clientId, nameof(ClientManager.ReceivePawnPosition), pawnPos);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveMoveCommand(Vector2I target)
        {
            long senderId = Multiplayer.GetRemoteSenderId();
            GD.Print($"Received move command from {senderId} to {target}");
            
            if (_gridSimulation.TryMovePawn(target))
            {
                GD.Print($"Pawn moved to {target}");
                BroadcastPawnPosition();
            }
            else
            {
                GD.Print($"Invalid move to {target}");
            }
        }

        private void BroadcastPawnPosition()
        {
            var pawnPos = _gridSimulation.GetPawnPosition();
            GD.Print($"[Server] BroadcastPawnPosition: Broadcasting pawn position {pawnPos} to all clients");
            Rpc(nameof(ClientManager.ReceivePawnPosition), pawnPos);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveGridSize(int size)
        {
            // This is called on clients, not server
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveTile(int x, int y, byte tileType)
        {
            // This is called on clients, not server
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveGridComplete()
        {
            // This is called on clients, not server
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceivePawnPosition(Vector2I position)
        {
            // This is called on clients, not server
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
