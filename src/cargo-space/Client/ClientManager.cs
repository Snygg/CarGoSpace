using Godot;
using CargoSpace.Core;
using System.Collections.Generic;
using CargoSpace.Shared;

namespace CargoSpace.Client
{
    public partial class ClientManager : Node
    {
        private ENetMultiplayerPeer _peer;
        private VisualGrid _visualGrid;
        private ColorRect _pawn;
        private CameraController _camera;
        private NetworkBridge _networkBridge;
        private Dictionary<Vector2I, GridTileData> _pendingGrid = new Dictionary<Vector2I, GridTileData>();
        private int _expectedTileCount = 0;

        public ClientManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            GameLogger.Debug("ClientManager._Ready() called");
            
            // Add camera first so everything else is visible
            _camera = new CameraController();
            AddChild(_camera);
            
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
            GameLogger.Debug("Starting client connection...");
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateClient(Constants.ServerAddress, Constants.ServerPort);
            
            if (error != Error.Ok)
            {
                GameLogger.Error($"Failed to start client: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
        }

        private void OnConnectedToServer()
        {
            GameLogger.Debug("Successfully connected to server!");
            // Request grid data from server via NetworkBridge
            _networkBridge.RequestGrid();
        }

        private void OnConnectionFailed()
        {
            GameLogger.Error("Failed to connect to server");
        }

        private void OnServerDisconnected()
        {
            GameLogger.Debug("Disconnected from server");
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
            GameLogger.Debug($"Clicked tile at {gridCoord}");
            
            // Send move command via NetworkBridge
            _networkBridge.SendMoveCommand(gridCoord);
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

        // Handler methods called by NetworkBridge RPCs
        public void HandleGridSize(int size)
        {
            GameLogger.Debug($"HandleGridSize: Expecting {size} tiles");
            _expectedTileCount = size;
            _pendingGrid.Clear();
        }

        public void HandleTile(int x, int y, byte tileType)
        {
            Vector2I coord = new Vector2I(x, y);
            _pendingGrid[coord] = new GridTileData((TileType)tileType);
            int count = _pendingGrid.Count;
            
            // Log every 10th tile to reduce spam
            if (count % 10 == 0)
            {
                GameLogger.Debug($"Progress: {count} tiles received so far");
            }
        }

        public void HandleGridComplete()
        {
            GameLogger.Debug($"HandleGridComplete: Received complete grid with {_pendingGrid.Count} tiles");
            _visualGrid.RenderGrid(_pendingGrid);
            
            // Center camera on the grid
            CenterCameraOnGrid();
        }

        private void CenterCameraOnGrid()
        {
            // Calculate grid bounds
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            foreach (var coord in _pendingGrid.Keys)
            {
                minX = Mathf.Min(minX, coord.X);
                maxX = Mathf.Max(maxX, coord.X);
                minY = Mathf.Min(minY, coord.Y);
                maxY = Mathf.Max(maxY, coord.Y);
            }
            
            int gridWidth = (maxX - minX + 1) * Constants.TileSize;
            int gridHeight = (maxY - minY + 1) * Constants.TileSize;
            
            GameLogger.Debug($"Centering camera on grid: {gridWidth}x{gridHeight}");
            _camera.CenterOnGrid(gridWidth, gridHeight);
        }

        public void HandlePawnPosition(Vector2I position)
        {
            GameLogger.Debug($"HandlePawnPosition: {position}");
            UpdatePawnVisual(position);
        }

        private void UpdatePawnVisual(Vector2I gridPosition)
        {
            // Position pawn at world coordinates (same as VisualGrid)
            Vector2 worldPosition = new Vector2(
                gridPosition.X * Constants.TileSize,
                gridPosition.Y * Constants.TileSize
            );
            
            _pawn.Position = worldPosition;
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
