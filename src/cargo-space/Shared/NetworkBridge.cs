using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
using CargoSpace.Client;

namespace CargoSpace.Shared
{
    public partial class NetworkBridge : Node
    {
        private ServerManager _serverManager;
        private ClientManager _clientManager;

        public void SetServerManager(ServerManager serverManager)
        {
            _serverManager = serverManager;
        }

        public void SetClientManager(ClientManager clientManager)
        {
            _clientManager = clientManager;
        }

        // Server-bound RPCs (called by clients)
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void RequestGrid_RPC()
        {
            GD.Print($"[NetworkBridge] RequestGrid_RPC received from {Multiplayer.GetRemoteSenderId()}");
            _serverManager?.HandleGridRequest(Multiplayer.GetRemoteSenderId());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveMoveCommand_RPC(Vector2I target)
        {
            GD.Print($"[NetworkBridge] ReceiveMoveCommand_RPC from {Multiplayer.GetRemoteSenderId()} to {target}");
            _serverManager?.HandleMoveCommand(target, Multiplayer.GetRemoteSenderId());
        }

        // Client-bound RPCs (called by server)
        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveGridSize_RPC(int size)
        {
            GD.Print($"[NetworkBridge] ReceiveGridSize_RPC: {size}");
            _clientManager?.HandleGridSize(size);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveTile_RPC(int x, int y, byte tileType)
        {
            _clientManager?.HandleTile(x, y, tileType);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveGridComplete_RPC()
        {
            GD.Print($"[NetworkBridge] ReceiveGridComplete_RPC");
            _clientManager?.HandleGridComplete();
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceivePawnPosition_RPC(Vector2I position)
        {
            GD.Print($"[NetworkBridge] ReceivePawnPosition_RPC: {position}");
            _clientManager?.HandlePawnPosition(position);
        }

        // Methods for managers to call RPCs
        public void SendGridSize(long clientId, int size)
        {
            RpcId(clientId, nameof(ReceiveGridSize_RPC), size);
        }

        public void SendTile(long clientId, int x, int y, byte tileType)
        {
            RpcId(clientId, nameof(ReceiveTile_RPC), x, y, tileType);
        }

        public void SendGridComplete(long clientId)
        {
            RpcId(clientId, nameof(ReceiveGridComplete_RPC));
        }

        public void SendPawnPosition(long clientId, Vector2I position)
        {
            RpcId(clientId, nameof(ReceivePawnPosition_RPC), position);
        }

        public void BroadcastPawnPosition(Vector2I position)
        {
            Rpc(nameof(ReceivePawnPosition_RPC), position);
        }

        public void RequestGrid()
        {
            RpcId(1, nameof(RequestGrid_RPC));
        }

        public void SendMoveCommand(Vector2I target)
        {
            RpcId(1, nameof(ReceiveMoveCommand_RPC), target);
        }
    }
}
