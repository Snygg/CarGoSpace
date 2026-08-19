using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class InputController : Node
    {
        public ClientDataCache DataCache;
        public CameraController Camera;
        public UIManager UIManager;
        public NetworkBridge NetworkBridge;

        private bool _isPaintingZone;
        private Vector2I _paintStart;

        public enum InputMode
        {
            Normal,
            PaintingZone,
            Blueprint
        }

        private InputMode _inputMode = InputMode.Normal;
        private byte _paintZoneType = 0;
        private byte _blueprintTargetTypeId = 0;

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
                            NetworkBridge?.SendToggleZoneTiles(tiles.ToArray(), _paintZoneType);
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
            else if (_inputMode == InputMode.Blueprint)
            {
                if (@event is InputEventMouseButton mouseEvent &&
                    mouseEvent.ButtonIndex == MouseButton.Left &&
                    mouseEvent.Pressed)
                {
                    Vector2I gridCoord = ScreenToGrid(mouseEvent.Position);
                    GameLogger.Debug($"Blueprint mode click at {gridCoord} for type {_blueprintTargetTypeId}");
                    NetworkBridge?.SendPlaceBlueprint(gridCoord, _blueprintTargetTypeId);
                    _inputMode = InputMode.Normal;
                }
                else if (@event is InputEventMouseButton rightEvent &&
                         rightEvent.ButtonIndex == MouseButton.Right &&
                         rightEvent.Pressed)
                {
                    _inputMode = InputMode.Normal;
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
            GameLogger.Debug($"InputController: entered painting mode for zone type {zoneType}");
        }

        public void SetBlueprintMode(byte targetTypeId)
        {
            _inputMode = InputMode.Blueprint;
            _blueprintTargetTypeId = targetTypeId;
            GameLogger.Debug($"InputController: entered blueprint mode for tile type {targetTypeId}");
        }

        private void HandleTileClick(Vector2 screenPosition)
        {
            Vector2I gridCoord = ScreenToGrid(screenPosition);
            GameLogger.Debug($"Clicked tile at {gridCoord}");

            if (DataCache != null && DataCache.TryGetTile(gridCoord, out GridTileData tileData))
            {
                TileDefinition tileDef = TileRegistry.Get(tileData.TypeId);
                if (tileDef != null && tileDef.IsInteractable)
                {
                    UIManager?.ShowContextMenu(gridCoord, tileDef, tileData.State, tileData.HazardState);
                    return;
                }
            }

            // Clear context slot if clicking elsewhere
            UIManager?.ClearContextMenu();
        }

        private Vector2I ScreenToGrid(Vector2 screenPosition)
        {
            // Get mouse position in world coordinates (accounting for camera)
            Camera2D camera = Camera ?? GetViewport().GetCamera2D();
            Vector2 worldMousePos = camera.GetGlobalMousePosition();

            // Convert to grid coordinates
            int gridX = Mathf.FloorToInt(worldMousePos.X / Constants.TileSize);
            int gridY = Mathf.FloorToInt(worldMousePos.Y / Constants.TileSize);

            return new Vector2I(gridX, gridY);
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
    }
}
