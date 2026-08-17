using Godot;
using CargoSpace.Core;
using System.Collections.Generic;
using CargoSpace.Server;

namespace CargoSpace.Client
{
    public partial class ClientManager : Node
    {
        private ENetMultiplayerPeer _peer;
        private VisualGrid _visualGrid;
        private ColorRect _pawn;
        private Dictionary<Vector2I, GridTileData> _pendingGrid = new Dictionary<Vector2I, GridTileData>();
        private int _expectedTileCount = 0;

        public override void _Ready()
        {
            _visualGrid = new VisualGrid();
            AddChild(_visualGrid);
            
            _pawn = new ColorRect();
            _pawn.Color = Colors.Red;
            _pawn.Size = new Vector2(Constants.TileSize, Constants.TileSize);
            _pawn.ZIndex = 10;
            AddChild(_pawn);
            
            StartClient();
        }

        private void StartClient()
        {
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateClient(Constants.ServerAddress, Constants.ServerPort);
            
            if (error != Error.Ok)
            {
                GD.PrintErr($"Failed to start client: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
            
            GD.Print($"Client connecting to {Constants.ServerAddress}:{Constants.ServerPort}");
        }

        private void OnConnectedToServer()
        {
            GD.Print("Connected to server");
        }

        private void OnConnectionFailed()
        {
            GD.PrintErr("Failed to connect to server");
        }

        private void OnServerDisconnected()
        {
            GD.Print("Disconnected from server");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                HandleTileClick(mouseEvent.Position);
            }
        }

        private void HandleTileClick(Vector2 screenPosition)
        {
            Vector2I gridCoord = ScreenToGrid(screenPosition);
            GD.Print($"Clicked tile at {gridCoord}");
            
            RpcId(1, nameof(ServerManager.ReceiveMoveCommand), gridCoord);
        }

        private Vector2I ScreenToGrid(Vector2 screenPosition)
        {
            // Center the grid on screen
            Vector2 viewportCenter = GetViewport().GetVisibleRect().Size / 2;
            Vector2 relativePosition = screenPosition - viewportCenter;
            
            // Convert to grid coordinates (assuming 5x5 grid centered at 0,0)
            int gridX = Mathf.FloorToInt(relativePosition.X / Constants.TileSize);
            int gridY = Mathf.FloorToInt(relativePosition.Y / Constants.TileSize);
            
            return new Vector2I(gridX, gridY);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveGridSize(int size)
        {
            GD.Print($"Expecting {size} tiles");
            _expectedTileCount = size;
            _pendingGrid.Clear();
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveTile(int x, int y, byte tileType)
        {
            Vector2I coord = new Vector2I(x, y);
            _pendingGrid[coord] = new GridTileData((TileType)tileType);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceiveGridComplete()
        {
            GD.Print($"Received complete grid with {_pendingGrid.Count} tiles");
            _visualGrid.RenderGrid(_pendingGrid);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void ReceivePawnPosition(Vector2I position)
        {
            GD.Print($"Received pawn position: {position}");
            UpdatePawnVisual(position);
        }

        private void UpdatePawnVisual(Vector2I gridPosition)
        {
            Vector2 viewportCenter = GetViewport().GetVisibleRect().Size / 2;
            Vector2 screenPosition = viewportCenter + new Vector2(
                gridPosition.X * Constants.TileSize,
                gridPosition.Y * Constants.TileSize
            );
            
            _pawn.Position = screenPosition;
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
