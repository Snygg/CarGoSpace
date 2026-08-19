using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class VisualGrid : Node2D
    {
        private TileMapLayer _tileMapLayer;
        private TileMapLayer _surfaceLayer;
        private TileMapLayer _hazardLayer;
        private TileMapLayer _zoneLayer;
        private ItemOverlayLayer _itemLayer;
        private Dictionary<Vector2I, List<string>> _groundItems = new();

        private TextureFactory _textureFactory;

        public ClientDataCache DataCache;

        public VisualGrid(TextureFactory textureFactory)
        {
            _textureFactory = textureFactory;
        }

        public override void _Ready()
        {
            // Create and configure the TileMapLayer
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.TileSet = _textureFactory.GetTileSet();
            _tileMapLayer.ZAsRelative = false;
            _tileMapLayer.ZIndex = -1;
            AddChild(_tileMapLayer);

            // Surface layer sits on top of the floor
            _surfaceLayer = new TileMapLayer();
            _surfaceLayer.TileSet = _textureFactory.GetTileSet();
            _surfaceLayer.ZIndex = 0;
            AddChild(_surfaceLayer);

            // Create the hazard overlay layer on top
            _hazardLayer = new TileMapLayer();
            _hazardLayer.TileSet = _textureFactory.GetHazardTileSet();
            AddChild(_hazardLayer);

            // Create the zone overlay layer between hazards and items
            _zoneLayer = new TileMapLayer();
            _zoneLayer.TileSet = _textureFactory.GetZoneTileSet();
            _zoneLayer.ZIndex = 1;
            AddChild(_zoneLayer);

            // Create the item overlay layer on top of everything
            _itemLayer = new ItemOverlayLayer();
            _itemLayer.GroundItemsRef = _groundItems;
            _itemLayer.ZIndex = 2;
            AddChild(_itemLayer);
        }

        public void UpdateGroundItems(Vector2I coord, List<string> items)
        {
            _groundItems[coord] = items;
            _itemLayer?.QueueRedraw();
        }

        public void UpdateTile(Vector2I coord, GridTileData tileData)
        {
            int sourceId = _textureFactory.GetSourceId(tileData.TypeId, tileData.State);
            _tileMapLayer?.SetCell(coord, sourceId, new Vector2I(0, 0));

            if (tileData.SurfaceTypeId != 0)
            {
                int surfaceSourceId = _textureFactory.GetSourceId(tileData.SurfaceTypeId, tileData.State);
                _surfaceLayer?.SetCell(coord, surfaceSourceId, new Vector2I(0, 0));
            }
            else
            {
                _surfaceLayer?.EraseCell(coord);
            }

            if (tileData.HazardState == 1)
            {
                _hazardLayer?.SetCell(coord, 1, new Vector2I(0, 0));
            }
            else
            {
                _hazardLayer?.EraseCell(coord);
            }
        }

        public void UpdateZones(IReadOnlyDictionary<Vector2I, ZoneType> zoneTiles)
        {
            _zoneLayer?.Clear();

            if (zoneTiles == null)
            {
                return;
            }

            foreach (var kvp in zoneTiles)
            {
                if (kvp.Value == ZoneType.None)
                    continue;

                int sourceId = _textureFactory.GetZoneSourceId(kvp.Value);
                _zoneLayer?.SetCell(kvp.Key, sourceId, new Vector2I(0, 0));
            }
        }

        public void RenderGrid(IReadOnlyDictionary<Vector2I, GridTileData> grid)
        {
            GameLogger.Debug($"RenderGrid called with {grid.Count} tiles");

            // Clear existing tiles
            _tileMapLayer.Clear();

            // Keep this Node2D at world origin
            Position = Vector2.Zero;

            // Set tiles based on grid data
            foreach (var kvp in grid)
            {
                Vector2I gridCoord = kvp.Key;
                GridTileData tileData = kvp.Value;
                int sourceId = _textureFactory.GetSourceId(tileData.TypeId, tileData.State);

                // Set the cell at the grid coordinate with the tile type's atlas coords
                _tileMapLayer.SetCell(gridCoord, sourceId, new Vector2I(0, 0));

                // Render surface overlay
                if (tileData.SurfaceTypeId != 0)
                {
                    int surfaceSourceId = _textureFactory.GetSourceId(tileData.SurfaceTypeId, tileData.State);
                    _surfaceLayer.SetCell(gridCoord, surfaceSourceId, new Vector2I(0, 0));
                }
                else
                {
                    _surfaceLayer.EraseCell(gridCoord);
                }

                // Render hazard overlay
                if (tileData.HazardState == 1)
                {
                    _hazardLayer.SetCell(gridCoord, 1, new Vector2I(0, 0));
                }
                else
                {
                    _hazardLayer.EraseCell(gridCoord);
                }
            }

            GameLogger.Debug($"Rendered {_tileMapLayer.GetUsedCells().Count} tiles total at world coordinates");
        }
    }
}
