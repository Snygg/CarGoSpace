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
        public void ReceiveJobCommand_RPC(byte[] jobIdBytes, Vector2I target, byte jobType)
        {
            JobId id = JobId.FromBytes(jobIdBytes);
            GameLogger.Debug($"ReceiveJobCommand_RPC from {Multiplayer.GetRemoteSenderId()} to {target}, type {jobType}, id {id}");
            _serverManager?.HandleJobCommand(id, target, (JobType)jobType, Multiplayer.GetRemoteSenderId());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveCancelJobRequest_RPC(byte[] jobIdBytes)
        {
            JobId id = JobId.FromBytes(jobIdBytes);
            GameLogger.Debug($"ReceiveCancelJobRequest_RPC from {Multiplayer.GetRemoteSenderId()} for job {id}");
            _serverManager?.HandleCancelJobRequest(id);
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
        public void ReceivePawnPosition_RPC(byte[] pawnIdBytes, Vector2I position)
        {
            PawnId id = PawnId.FromBytes(pawnIdBytes);
            GameLogger.Debug($"ReceivePawnPosition_RPC: {id} at {position}");
            _clientManager?.HandlePawnPosition(id, position);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveJobAdded_RPC(byte[] jobIdBytes, Vector2I target, byte jobType)
        {
            JobId id = JobId.FromBytes(jobIdBytes);
            GameLogger.Debug($"ReceiveJobAdded_RPC: {id} at {target}, type {jobType}");
            _clientManager?.HandleJobAdded(id, target, (JobType)jobType);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveJobRemoved_RPC(byte[] jobIdBytes)
        {
            JobId id = JobId.FromBytes(jobIdBytes);
            GameLogger.Debug($"ReceiveJobRemoved_RPC: {id}");
            _clientManager?.HandleJobRemoved(id);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void ReceiveJobRejected_RPC(byte[] jobIdBytes)
        {
            JobId id = JobId.FromBytes(jobIdBytes);
            GameLogger.Debug($"ReceiveJobRejected_RPC: {id}");
            _clientManager?.HandleJobRejected(id);
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

        public void SendPawnPosition(long clientId, PawnId pawnId, Vector2I position)
        {
            RpcId(clientId, nameof(ReceivePawnPosition_RPC), pawnId.ToNetworkBytes(), position);
        }

        public void BroadcastPawnPosition(PawnId pawnId, Vector2I position)
        {
            Rpc(nameof(ReceivePawnPosition_RPC), pawnId.ToNetworkBytes(), position);
        }

        public void BroadcastTileUpdate(Vector2I coord, GridTileData tileData)
        {
            Rpc(nameof(ReceiveTile_RPC), coord.X, coord.Y, (byte)tileData.Type, tileData.State);
        }

        public void BroadcastJobAdded(Job job)
        {
            // Broadcast to all clients except the originating client (already optimistic)
            foreach (long peerId in Multiplayer.GetPeers())
            {
                if (peerId != job.OwnerPeerId)
                {
                    RpcId(peerId, nameof(ReceiveJobAdded_RPC), job.Id.ToNetworkBytes(), job.Target, (byte)job.Type);
                }
            }
        }

        public void BroadcastJobRemoved(JobId id)
        {
            Rpc(nameof(ReceiveJobRemoved_RPC), id.ToNetworkBytes());
        }

        public void SendJobRejected(JobId id, long peerId)
        {
            RpcId(peerId, nameof(ReceiveJobRejected_RPC), id.ToNetworkBytes());
        }

        public void RequestGrid()
        {
            RpcId(1, nameof(RequestGrid_RPC));
        }

        public void SendJobCommand(JobId jobId, Vector2I target, JobType jobType)
        {
            RpcId(1, nameof(ReceiveJobCommand_RPC), jobId.ToNetworkBytes(), target, (byte)jobType);
        }

        public void SendCancelJobRequest(JobId jobId)
        {
            RpcId(1, nameof(ReceiveCancelJobRequest_RPC), jobId.ToNetworkBytes());
        }
    }
}
