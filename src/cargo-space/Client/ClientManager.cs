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
        private CameraController _camera;
        private UIManager _uiManager;
        private Starfield _starfield;
        private FlotsamVisualLayer _flotsamVisualLayer;
        private HarpoonLayer _harpoonLayer;
        private NetworkBridge _networkBridge;
        private Dictionary<Vector2I, GridTileData> _pendingGrid = new Dictionary<Vector2I, GridTileData>();
        private List<Job> _activeJobs = new List<Job>();
        private Dictionary<PawnId, PawnVisual> _pawnVisuals = new Dictionary<PawnId, PawnVisual>();
        private int _expectedTileCount = 0;
        private bool _gridRendered = false;
        private Dictionary<Vector2I, List<string>> _clientGroundItems = new();
        private Dictionary<Vector2I, ZoneType> _clientZoneTiles = new();

        private bool _isPaintingZone;
        private Vector2I _paintStart;

        public enum InputMode
        {
            Normal,
            PaintingZone
        }

        private InputMode _inputMode = InputMode.Normal;
        private byte _paintZoneType = 0;

        public ClientManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            GameLogger.Debug("ClientManager._Ready() called");
            
            // Starfield first so it renders behind everything
            _starfield = new Starfield();
            _starfield.ZIndex = -10;
            AddChild(_starfield);
            
            // Add camera first so everything else is visible
            _camera = new CameraController();
            AddChild(_camera);

            _flotsamVisualLayer = new FlotsamVisualLayer();
            _flotsamVisualLayer.CameraRef = _camera;
            _flotsamVisualLayer.ZIndex = -1;
            AddChild(_flotsamVisualLayer);
            
            _visualGrid = new VisualGrid();
            AddChild(_visualGrid);
            
            _harpoonLayer = new HarpoonLayer();
            _harpoonLayer.GridRef = _pendingGrid;
            _harpoonLayer.ZIndex = 1;
            AddChild(_harpoonLayer);
            
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

        public override void _Process(double delta)
        {
            if (!_gridRendered)
            {
                return;
            }

            Rect2 visibleRect = GetVisibleWorldRect();

            foreach (var kvp in _pendingGrid)
            {
                Vector2I coord = kvp.Key;
                GridTileData tileData = kvp.Value;
                _uiManager.TryDiscoverHazard(coord, tileData.HazardState, IsCoordVisible(coord, visibleRect));
            }
        }

        private Rect2 GetVisibleWorldRect()
        {
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            Vector2 cameraPosition = _camera.Position;
            Vector2 zoom = _camera.Zoom;
            Vector2 visibleSize = viewportSize / zoom;

            return new Rect2(cameraPosition - visibleSize / 2, visibleSize);
        }

        private bool IsCoordVisible(Vector2I coord, Rect2 visibleRect)
        {
            Vector2 worldMin = new Vector2(coord.X * Constants.TileSize, coord.Y * Constants.TileSize);
            Vector2 worldMax = worldMin + new Vector2(Constants.TileSize, Constants.TileSize);
            Rect2 tileRect = new Rect2(worldMin, worldMax - worldMin);

            return visibleRect.Intersects(tileRect);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_inputMode == InputMode.PaintingZone)
            {
                if (@event is InputEventMouseButton mouseBtn)
                {
                    if (mouseBtn.ButtonIndex == MouseButton.Left)
                    {
                        if (mouseBtn.Pressed)
                        {
                            _isPaintingZone = true;
                            _paintStart = ScreenToGrid(mouseBtn.Position);
                        }
                        else if (_isPaintingZone)
                        {
                            _isPaintingZone = false;
                            List<Vector2I> tiles = GetTilesInRect(_paintStart, ScreenToGrid(mouseBtn.Position));
                            _networkBridge?.SendToggleZoneTiles(tiles.ToArray(), _paintZoneType);
                            _inputMode = InputMode.Normal;
                        }
                    }
                    else if (mouseBtn.ButtonIndex == MouseButton.Right && mouseBtn.Pressed)
                    {
                        _isPaintingZone = false;
                        _inputMode = InputMode.Normal;
                    }
                }
            }
            else // Normal mode
            {
                if (@event is InputEventMouseButton mouseEvent &&
                    mouseEvent.ButtonIndex == MouseButton.Left &&
                    mouseEvent.Pressed)
                {
                    HandleTileClick(mouseEvent.Position);
                }
            }
        }

        public void SetPaintingMode(byte zoneType)
        {
            _inputMode = InputMode.PaintingZone;
            _paintZoneType = zoneType;
            GameLogger.Debug($"ClientManager: entered painting mode for zone type {zoneType}");
        }

        private void HandleTileClick(Vector2 screenPosition)
        {
            Vector2I gridCoord = ScreenToGrid(screenPosition);
            GameLogger.Debug($"Clicked tile at {gridCoord}");
            
            if (_pendingGrid.TryGetValue(gridCoord, out GridTileData tileData))
            {
                TileDefinition tileDef = TileRegistry.Get(tileData.TypeId);
                if (tileDef != null && tileDef.IsInteractable)
                {
                    // Show context menu for the clicked interactable tile
                    _uiManager.ShowContextMenu(gridCoord, tileDef, tileData.State, tileData.HazardState);
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

        public void HandleTile(int x, int y, byte tileType, int state, byte hazardState)
        {
            Vector2I coord = new Vector2I(x, y);
            _pendingGrid[coord] = new GridTileData(tileType, state, hazardState);
            int count = _pendingGrid.Count;
            
            // Log every 10th tile to reduce spam
            if (count % 10 == 0)
            {
                GameLogger.Debug($"Progress: {count} tiles received so far");
            }

            // Let the triage list react to the current hazard state (off-screen check is false here).
            _uiManager.TryDiscoverHazard(coord, hazardState, false);
            
            // If the grid has already been rendered, re-render to reflect state changes
            if (_gridRendered)
            {
                _visualGrid.RenderGrid(_pendingGrid);
                _harpoonLayer.QueueRedraw();
            }
        }

        public void HandleGridComplete()
        {
            GameLogger.Debug($"HandleGridComplete: Received complete grid with {_pendingGrid.Count} tiles");
            _visualGrid.RenderGrid(_pendingGrid);
            _harpoonLayer.QueueRedraw();
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

        public void HandleEntityPosition(EntityType entityType, byte[] idBytes, Vector2I position)
        {
            GameLogger.Debug($"HandleEntityPosition: type {entityType} at {position}");

            if (entityType == EntityType.Pawn)
            {
                PawnId id = PawnId.FromBytes(idBytes);
                UpdatePawnVisual(id, position);
            }
        }

        public void HandleJobAdded(JobId id, Vector2I target, JobType jobType)
        {
            GameLogger.Debug($"HandleJobAdded: {id} at {target}, type {jobType}");

            // Ignore if we already have this job (e.g. from our own optimistic add)
            if (FindActiveJob(id) != null)
            {
                return;
            }

            Job job = new Job(id, 0, target, jobType, 0);
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

        public void HandleHarpoonCatch(Vector2I coord, string itemId)
        {
            GameLogger.Debug($"HandleHarpoonCatch: {itemId} at {coord}");
            _flotsamVisualLayer?.PlayCatchEffect(coord, itemId);
        }

        public void HandleGroundItemsUpdate(Vector2I coord, string[] items)
        {
            GameLogger.Debug($"HandleGroundItemsUpdate: {items?.Length ?? 0} items at {coord}");
            _clientGroundItems[coord] = new List<string>(items ?? new string[0]);
            _visualGrid?.UpdateGroundItems(coord, _clientGroundItems[coord]);
        }

        public void HandleZoneUpdate(Vector2I[] tiles, byte[] types)
        {
            int count = tiles?.Length ?? 0;
            GameLogger.Debug($"HandleZoneUpdate: {count} zone tiles");

            _clientZoneTiles = new Dictionary<Vector2I, ZoneType>();
            if (tiles != null && types != null)
            {
                for (int i = 0; i < count; i++)
                {
                    _clientZoneTiles[tiles[i]] = (ZoneType)types[i];
                }
            }

            _visualGrid?.UpdateZones(_clientZoneTiles);
        }

        private List<Vector2I> GetTilesInRect(Vector2I start, Vector2I end)
        {
            List<Vector2I> tiles = new List<Vector2I>();
            int minX = Mathf.Min(start.X, end.X);
            int maxX = Mathf.Max(start.X, end.X);
            int minY = Mathf.Min(start.Y, end.Y);
            int maxY = Mathf.Max(start.Y, end.Y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    tiles.Add(new Vector2I(x, y));
                }
            }

            return tiles;
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

        private void UpdatePawnVisual(PawnId id, Vector2I gridPosition)
        {
            if (!_pawnVisuals.ContainsKey(id))
            {
                PawnVisual pawnVisual = new PawnVisual();
                pawnVisual.ZIndex = 10;
                AddChild(pawnVisual);
                _pawnVisuals[id] = pawnVisual;
            }

            PawnVisual visual = _pawnVisuals[id];

            // Position pawn at world coordinates (same as VisualGrid)
            Vector2 worldPosition = new Vector2(
                gridPosition.X * Constants.TileSize,
                gridPosition.Y * Constants.TileSize
            );
            
            visual.Position = worldPosition;
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
