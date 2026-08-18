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
            GameLogger.Debug($"RequestGrid_RPC received from {Multiplayer.GetRemoteSenderId()}");
            _serverManager?.HandleGridRequest(Multiplayer.GetRemoteSenderId());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveJobCommand_RPC(Vector2I target, byte jobType)
        {
            GameLogger.Debug($"ReceiveJobCommand_RPC from {Multiplayer.GetRemoteSenderId()} to {target}, type {jobType}");
            _serverManager?.HandleJobCommand(target, (JobType)jobType, Multiplayer.GetRemoteSenderId());
        }

        // Client-bound RPCs (called by server)
        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveGridSize_RPC(int size)
        {
            GameLogger.Debug($"ReceiveGridSize_RPC: {size}");
            _clientManager?.HandleGridSize(size);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveTile_RPC(int x, int y, byte tileType, int state)
        {
            _clientManager?.HandleTile(x, y, tileType, state);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveGridComplete_RPC()
        {
            GameLogger.Debug("ReceiveGridComplete_RPC");
            _clientManager?.HandleGridComplete();
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceivePawnPosition_RPC(Vector2I position)
        {
            GameLogger.Debug($"ReceivePawnPosition_RPC: {position}");
            _clientManager?.HandlePawnPosition(position);
        }

        // Methods for managers to call RPCs
        public void SendGridSize(long clientId, int size)
        {
            RpcId(clientId, nameof(ReceiveGridSize_RPC), size);
        }

        public void SendTile(long clientId, int x, int y, byte tileType, int state)
        {
            RpcId(clientId, nameof(ReceiveTile_RPC), x, y, tileType, state);
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

        public void BroadcastTileUpdate(Vector2I coord, GridTileData tileData)
        {
            Rpc(nameof(ReceiveTile_RPC), coord.X, coord.Y, (byte)tileData.Type, tileData.State);
        }

        public void RequestGrid()
        {
            RpcId(1, nameof(RequestGrid_RPC));
        }

        public void SendJobCommand(Vector2I target, JobType jobType)
        {
            RpcId(1, nameof(ReceiveJobCommand_RPC), target, (byte)jobType);
        }
    }
}
