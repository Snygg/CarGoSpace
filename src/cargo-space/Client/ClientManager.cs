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
        private GridHighlighter _gridHighlighter;
        private TextureFactory _textureFactory;
        private ClientConstructionValidator _validator;
        private CameraController _camera;
        private UIManager _uiManager;
        private Starfield _starfield;
        private FlotsamVisualLayer _flotsamVisualLayer;
        private HarpoonLayer _harpoonLayer;
        private NetworkBridge _networkBridge;
        private Dictionary<PawnId, PawnVisual> _pawnVisuals = new();

        private ClientDataCache _dataCache;
        private InputController _inputController;

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
            _flotsamVisualLayer.StarfieldRef = _starfield;
            _flotsamVisualLayer.ZIndex = -1;
            AddChild(_flotsamVisualLayer);

            _dataCache = new ClientDataCache();
            _textureFactory = new TextureFactory();
            _validator = new ClientConstructionValidator(_dataCache);

            _visualGrid = new VisualGrid(_textureFactory);
            _visualGrid.DataCache = _dataCache;
            AddChild(_visualGrid);

            _gridHighlighter = new GridHighlighter(_dataCache, _validator, _textureFactory);
            _gridHighlighter.ZIndex = 10;
            AddChild(_gridHighlighter);

            _harpoonLayer = new HarpoonLayer();
            _harpoonLayer.ZIndex = 1;
            AddChild(_harpoonLayer);

            _uiManager = new UIManager();
            _uiManager.Initialize(this, _networkBridge);
            AddChild(_uiManager);

            _inputController = new InputController();
            _inputController.DataCache = _dataCache;
            _inputController.Camera = _camera;
            _inputController.UIManager = _uiManager;
            _inputController.NetworkBridge = _networkBridge;
            _inputController.ClientManager = this;
            AddChild(_inputController);

            _harpoonLayer.GridRef = _dataCache.Grid;

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
            if (!_dataCache.IsGridRendered || _dataCache.ActiveHazards.Count == 0)
            {
                return;
            }

            Rect2 visibleRect = GetVisibleWorldRect();

            // Only check intersection math for tiles that actually have hazards!
            foreach (Vector2I coord in _dataCache.ActiveHazards)
            {
                if (_dataCache.TryGetTile(coord, out GridTileData tileData))
                {
                    _uiManager.TryDiscoverHazard(coord, tileData.HazardState, IsCoordVisible(coord, visibleRect));
                }
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

        public void SetPaintingMode(byte zoneType)
        {
            _inputController?.SetPaintingMode(zoneType);
        }

        public void SetBlueprintMode(byte targetTypeId)
        {
            _inputController?.SetBlueprintMode(targetTypeId);
        }

        public void CancelBlueprintMode()
        {
            _inputController?.CancelBlueprintMode();
        }

        public void QueueHighlightRedraw()
        {
            _gridHighlighter?.QueueRedraw();
        }

        // Handler methods called by NetworkBridge RPCs
        public void HandleGridSize(int size)
        {
            GameLogger.Debug($"HandleGridSize: Expecting {size} tiles");
            _dataCache.ExpectedTileCount = size;
            _dataCache.ClearGrid();
            _dataCache.IsGridRendered = false;
        }

        public void HandleTile(int x, int y, byte tileType, int state, byte hazardState, byte surfaceTypeId)
        {
            Vector2I coord = new Vector2I(x, y);
            GridTileData data = new GridTileData(tileType, surfaceTypeId, state, hazardState);
            _dataCache.UpdateTile(coord, data);

            int count = _dataCache.Grid.Count;

            // Log every 10th tile to reduce spam
            if (count % 10 == 0)
            {
                GameLogger.Debug($"Progress: {count} tiles received so far");
            }

            // Let the triage list react to the current hazard state (off-screen check is false here).
            _uiManager.TryDiscoverHazard(coord, data.HazardState, false);

            // If the grid has already been rendered, update this tile in place
            if (_dataCache.IsGridRendered)
            {
                _visualGrid?.UpdateTile(coord, data);
                _harpoonLayer?.QueueRedraw();
            }
        }

        public void HandleGridComplete()
        {
            GameLogger.Debug($"HandleGridComplete: Received complete grid with {_dataCache.Grid.Count} tiles");
            _visualGrid?.RenderGrid(_dataCache.Grid);
            _dataCache.RecalculateRegions();
            _harpoonLayer?.QueueRedraw();
            _dataCache.IsGridRendered = true;

            // Center camera on the grid
            CenterCameraOnGrid();
        }

        private void CenterCameraOnGrid()
        {
            // Calculate grid bounds
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var coord in _dataCache.Grid.Keys)
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
            if (_dataCache.TryFindJob(id, out _))
            {
                return;
            }

            Job job = new Job(id, 0, target, jobType, 0);
            _dataCache.AddJob(job);
            _uiManager.AddJobUI(id, jobType, target);
        }

        public void HandleJobRemoved(JobId id)
        {
            GameLogger.Debug($"HandleJobRemoved: {id}");
            _dataCache.RemoveJob(id);
            _uiManager.RemoveJobUI(id);
        }

        public void HandleJobRejected(JobId id)
        {
            GameLogger.Debug($"HandleJobRejected: {id}");
            _dataCache.RemoveJob(id);
            _uiManager.RemoveJobUI(id);
        }

        public void HandleHarpoonCatch(Vector2I coord, string itemId)
        {
            GameLogger.Debug($"HandleHarpoonCatch: {itemId} at {coord}");
            _flotsamVisualLayer?.PlayCatchEffect(coord, itemId);
        }

        public void HandleMachineStateUpdate(Vector2I coord, string key, float value)
        {
            GameLogger.Debug($"Machine {coord} updated {key} to {value}");
        }

        public void HandleBlueprintState(Vector2I coord, byte targetTypeId, string[] reqKeys, int[] reqVals, string[] delKeys, int[] delVals)
        {
            GameLogger.Debug($"HandleBlueprintState: target {targetTypeId} at {coord}");

            if (reqKeys == null || reqKeys.Length == 0)
            {
                _dataCache.RemoveBlueprint(coord);
                _gridHighlighter?.QueueRedraw();
                return;
            }

            Blueprint bp = new Blueprint
            {
                Position = coord,
                TargetTypeId = targetTypeId,
                Required = new(),
                Delivered = new()
            };

            if (reqKeys != null && reqVals != null)
            {
                for (int i = 0; i < reqKeys.Length; i++)
                {
                    if (i < reqVals.Length)
                        bp.Required[reqKeys[i]] = reqVals[i];
                }
            }

            if (delKeys != null && delVals != null)
            {
                for (int i = 0; i < delKeys.Length; i++)
                {
                    if (i < delVals.Length)
                        bp.Delivered[delKeys[i]] = delVals[i];
                }
            }

            _dataCache.UpdateBlueprint(coord, bp);
            _gridHighlighter?.QueueRedraw();
        }

        public void HandlePlaceBlueprint(Vector2I coord, byte targetTypeId)
        {
            _networkBridge?.SendPlaceBlueprint(coord, targetTypeId);
        }

        public void HandleGroundItemsUpdate(Vector2I coord, string[] items)
        {
            GameLogger.Debug($"HandleGroundItemsUpdate: {items?.Length ?? 0} items at {coord}");

            List<string> itemList = new List<string>(items ?? new string[0]);
            _dataCache.UpdateGroundItems(coord, itemList);
            _visualGrid?.UpdateGroundItems(coord, itemList);
        }

        public void HandleZoneUpdate(Vector2I[] tiles, byte[] types)
        {
            int count = tiles?.Length ?? 0;
            GameLogger.Debug($"HandleZoneUpdate: {count} zone tiles");

            _dataCache.ClearZones();
            if (tiles != null && types != null)
            {
                for (int i = 0; i < count; i++)
                {
                    _dataCache.UpdateZone(tiles[i], (ZoneType)types[i]);
                }
            }

            _visualGrid?.UpdateZones(_dataCache.GetAllZones());
        }

        public void HandleRegionAtmosphere(Vector2I safeTile, byte oxygen, byte smoke)
        {
            GameLogger.Debug($"HandleRegionAtmosphere: safeTile={safeTile}, oxygen={oxygen}, smoke={smoke}");

            // Keep the client region graph in sync before resolving the safe tile.
            _dataCache.RecalculateRegions();

            if (!_dataCache.TryGetRegion(safeTile, out int regionId))
            {
                GameLogger.Warning($"HandleRegionAtmosphere: no client region for safeTile {safeTile}");
                return;
            }

            if (!_dataCache.TryGetRegionTiles(regionId, out var tiles))
            {
                GameLogger.Warning($"HandleRegionAtmosphere: no tiles for region {regionId}");
                return;
            }

            int? sourceId = null;

            if (smoke > 0)
            {
                sourceId = _textureFactory.GetAtmosphereSourceId("Smoke");
            }
            else if (oxygen == 0)
            {
                sourceId = _textureFactory.GetAtmosphereSourceId("ZeroOxygen");
            }
            else if (oxygen == 1)
            {
                sourceId = _textureFactory.GetAtmosphereSourceId("LowOxygen");
            }

            _visualGrid?.UpdateAtmosphere(sourceId, tiles);
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
