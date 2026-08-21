using Godot;
using CargoSpace.Core;
using System;

namespace CargoSpace.Client
{
    public partial class NetworkClient : Node
    {
        private ENetMultiplayerPeer _peer;

        public event Action OnConnectedToServer;
        public event Action OnConnectionFailed;
        public event Action OnServerDisconnected;

        public void StartClient()
        {
            GameLogger.Debug("Starting client connection...");
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateClient(Constants.ServerAddress, Constants.ServerPort);

            if (error != Error.Ok)
            {
                GameLogger.Error($"Failed to start client: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.ConnectedToServer += () => OnConnectedToServer?.Invoke();
            Multiplayer.ConnectionFailed += () => OnConnectionFailed?.Invoke();
            Multiplayer.ServerDisconnected += () => OnServerDisconnected?.Invoke();
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
