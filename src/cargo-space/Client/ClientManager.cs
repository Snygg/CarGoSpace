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
        private Camera2D _camera;
        private NetworkBridge _networkBridge;
        private Dictionary<Vector2I, GridTileData> _pendingGrid = new Dictionary<Vector2I, GridTileData>();
        private int _expectedTileCount = 0;

        public ClientManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            GD.Print("[Client] ClientManager._Ready() called");
            
            // Add camera first so everything else is visible
            _camera = new Camera2D();
            _camera.Position = new Vector2(0, 0);
            AddChild(_camera);
            GD.Print("[Client] Camera2D added at position (0,0)");
            
            _visualGrid = new VisualGrid();
            AddChild(_visualGrid);
            GD.Print("[Client] VisualGrid added to scene tree");
            
            _pawn = new ColorRect();
            _pawn.Color = Colors.Red;
            _pawn.Size = new Vector2(Constants.TileSize, Constants.TileSize);
            _pawn.ZIndex = 10;
            AddChild(_pawn);
            GD.Print("[Client] Pawn added to scene tree");
            
            StartClient();
        }

        private void StartClient()
        {
            GD.Print("[Client] Starting client connection...");
            _peer = new ENetMultiplayerPeer();
            var error = _peer.CreateClient(Constants.ServerAddress, Constants.ServerPort);
            
            if (error != Error.Ok)
            {
                GD.PrintErr($"[Client] Failed to start client: {error}");
                return;
            }

            Multiplayer.MultiplayerPeer = _peer;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
            
            GD.Print($"[Client] Connecting to {Constants.ServerAddress}:{Constants.ServerPort}");
        }

        private void OnConnectedToServer()
        {
            GD.Print("[Client] Successfully connected to server!");
            // Request grid data from server via NetworkBridge
            GD.Print("[Client] Requesting grid data from server...");
            _networkBridge.RequestGrid();
        }

        private void OnConnectionFailed()
        {
            GD.PrintErr("[Client] Failed to connect to server");
        }

        private void OnServerDisconnected()
        {
            GD.Print("[Client] Disconnected from server");
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
            GD.Print($"[Client] Clicked tile at {gridCoord}");
            
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
            GD.Print($"[Client] HandleGridSize: Expecting {size} tiles");
            _expectedTileCount = size;
            _pendingGrid.Clear();
        }

        public void HandleTile(int x, int y, byte tileType)
        {
            Vector2I coord = new Vector2I(x, y);
            _pendingGrid[coord] = new GridTileData((TileType)tileType);
            int count = _pendingGrid.Count;
            GD.Print($"[Client] HandleTile: Received tile at ({x}, {y}) type: {tileType}, total tiles: {count}");
            
            // Log every 10th tile to reduce spam
            if (count % 10 == 0)
            {
                GD.Print($"[Client] Progress: {count} tiles received so far");
            }
        }

        public void HandleGridComplete()
        {
            GD.Print($"[Client] HandleGridComplete: Received complete grid with {_pendingGrid.Count} tiles");
            _visualGrid.RenderGrid(_pendingGrid);
        }

        public void HandlePawnPosition(Vector2I position)
        {
            GD.Print($"[Client] HandlePawnPosition: {position}");
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
