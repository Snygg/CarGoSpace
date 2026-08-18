using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
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
        private UIManager _uiManager;
        private NetworkBridge _networkBridge;
        private Dictionary<Vector2I, GridTileData> _pendingGrid = new Dictionary<Vector2I, GridTileData>();
        private List<Job> _activeJobs = new List<Job>();
        private int _expectedTileCount = 0;
        private bool _gridRendered = false;

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
            
            _uiManager = new UIManager();
            _uiManager.Initialize(this, _networkBridge);
            AddChild(_uiManager);
            
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

        public override void _UnhandledInput(InputEvent @event)
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
            
            if (_pendingGrid.TryGetValue(gridCoord, out GridTileData tileData))
            {
                TileDefinition tileDef = TileRegistry.Get(tileData.Type);
                if (tileDef != null && tileDef.IsInteractable)
                {
                    // Show context menu for the clicked interactable tile
                    _uiManager.ShowContextMenu(gridCoord, tileDef, tileData.State);
                    return;
                }
            }
            
            // Clear context slot if clicking elsewhere
            _uiManager.ClearContextMenu();
        }

        private Vector2I ScreenToGrid(Vector2 screenPosition)
        {
            // Get mouse position in world coordinates (accounting for camera)
            Camera2D camera = GetViewport().GetCamera2D();
            Vector2 worldMousePos = camera.GetGlobalMousePosition();
            
            // Convert to grid coordinates
            int gridX = Mathf.FloorToInt(worldMousePos.X / Constants.TileSize);
            int gridY = Mathf.FloorToInt(worldMousePos.Y / Constants.TileSize);
            
            return new Vector2I(gridX, gridY);
        }

        // Handler methods called by NetworkBridge RPCs
        public void HandleGridSize(int size)
        {
            GameLogger.Debug($"HandleGridSize: Expecting {size} tiles");
            _expectedTileCount = size;
            _pendingGrid.Clear();
        }

        public void HandleTile(int x, int y, byte tileType, int state)
        {
            Vector2I coord = new Vector2I(x, y);
            _pendingGrid[coord] = new GridTileData((TileType)tileType, state);
            int count = _pendingGrid.Count;
            
            // Log every 10th tile to reduce spam
            if (count % 10 == 0)
            {
                GameLogger.Debug($"Progress: {count} tiles received so far");
            }
            
            // If the grid has already been rendered, re-render to reflect state changes
            if (_gridRendered)
            {
                _visualGrid.RenderGrid(_pendingGrid);
            }
        }

        public void HandleGridComplete()
        {
            GameLogger.Debug($"HandleGridComplete: Received complete grid with {_pendingGrid.Count} tiles");
            _visualGrid.RenderGrid(_pendingGrid);
            _gridRendered = true;
            
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

        public void HandleJobAdded(JobId id, Vector2I target, JobType jobType)
        {
            GameLogger.Debug($"HandleJobAdded: {id} at {target}, type {jobType}");

            // Ignore if we already have this job (e.g. from our own optimistic add)
            if (FindActiveJob(id) != null)
            {
                return;
            }

            Job job = new Job(id, 0, target, jobType);
            _activeJobs.Add(job);
            _uiManager.AddJobUI(id, jobType, target);
        }

        public void HandleJobRemoved(JobId id)
        {
            GameLogger.Debug($"HandleJobRemoved: {id}");
            RemoveActiveJob(id);
            _uiManager.RemoveJobUI(id);
        }

        public void HandleJobRejected(JobId id)
        {
            GameLogger.Debug($"HandleJobRejected: {id}");
            RemoveActiveJob(id);
            _uiManager.RemoveJobUI(id);
        }

        public void QueueConsoleToggleJob(Vector2I target)
        {
            JobId jobId = JobId.Create();
            long ownerPeerId = Multiplayer.GetUniqueId();
            Job job = new Job(jobId, ownerPeerId, target, JobType.ToggleState);

            // Optimistic UI: immediately show on client before server validation
            _activeJobs.Add(job);
            _uiManager.AddJobUI(jobId, JobType.ToggleState, target);

            // Send to server for validation and queueing
            _networkBridge?.SendJobCommand(jobId, target, JobType.ToggleState);
        }

        private Job? FindActiveJob(JobId id)
        {
            foreach (Job job in _activeJobs)
            {
                if (job.Id == id)
                {
                    return job;
                }
            }
            return null;
        }

        private void RemoveActiveJob(JobId id)
        {
            for (int i = 0; i < _activeJobs.Count; i++)
            {
                if (_activeJobs[i].Id == id)
                {
                    _activeJobs.RemoveAt(i);
                    return;
                }
            }
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
