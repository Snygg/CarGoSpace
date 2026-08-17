using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Server
{
    public partial class ServerManager : Node
    {
        private ENetMultiplayerPeer _peer;
        private GridSimulation _gridSimulation;

        public override void _Ready()
        {
            _gridSimulation = new GridSimulation();
            StartServer();
        }

        private void StartServer()
        {
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateServer(Constants.ServerPort, 32);
            
            if (error != Error.Ok)
            {
                GD.PrintErr($"Failed to start server: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            
            GD.Print($"Server started on port {Constants.ServerPort}");
        }

        private void OnPeerConnected(long id)
        {
            GD.Print($"Client connected: {id}");
            SendGridToClient(id);
            SendPawnPositionToClient(id);
        }

        private void OnPeerDisconnected(long id)
        {
            GD.Print($"Client disconnected: {id}");
        }

        private void SendGridToClient(long clientId)
        {
            var grid = _gridSimulation.GetGrid();
            
            // Send grid size first
            RpcId(clientId, nameof(ReceiveGridSize), grid.Count);
            
            // Send each tile individually
            foreach (var kvp in grid)
            {
                RpcId(clientId, nameof(ReceiveTile), kvp.Key.X, kvp.Key.Y, (byte)kvp.Value.Type);
            }
            
            // Signal that grid transfer is complete
            RpcId(clientId, nameof(ReceiveGridComplete));
        }

        private void SendPawnPositionToClient(long clientId)
        {
            var pawnPos = _gridSimulation.GetPawnPosition();
            RpcId(clientId, nameof(ReceivePawnPosition), pawnPos);
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
            Rpc(nameof(ReceivePawnPosition), pawnPos);
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
