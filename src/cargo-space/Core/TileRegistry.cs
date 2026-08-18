using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CargoSpace.Core
{
    public class TileDefinition
    {
        public TileType Type { get; set; }
        public string Name { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string HexColor { get; set; }

        public bool IsWalkable => Tags != null && Tags.Contains("Walkable");
        public bool IsInteractable => Tags != null && Tags.Contains("Interactable");

        public bool HasTag(string tag)
        {
            return Tags != null && Tags.Contains(tag);
        }

        public Color GetColor()
        {
            return Color.FromHtml(HexColor);
        }
    }

    public static class TileRegistry
    {
        private static Dictionary<TileType, TileDefinition> _tiles;
        private static bool _isLoaded = false;

        public static void LoadFromFile(string filePath)
        {
            if (!FileAccess.FileExists(filePath))
            {
                GameLogger.Error($"Tile registry file not found: {filePath}");
                return;
            }

            using FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            LoadFromJson(json);
        }

        public static void LoadFromJson(string json)
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, TileDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _tiles = new Dictionary<TileType, TileDefinition>();
            foreach (var kvp in parsed)
            {
                _tiles[kvp.Value.Type] = kvp.Value;
            }

            _isLoaded = true;
            GameLogger.Debug($"TileRegistry loaded {_tiles.Count} tile definitions");
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                GameLogger.Warning("TileRegistry accessed before loading. Call LoadFromFile() during game startup.");
            }
        }

        public static TileDefinition Get(TileType type)
        {
            EnsureLoaded();
            if (_tiles != null && _tiles.TryGetValue(type, out TileDefinition definition))
                return definition;
            
            return null;
        }

        public static bool TryGet(TileType type, out TileDefinition definition)
        {
            EnsureLoaded();
            if (_tiles != null)
                return _tiles.TryGetValue(type, out definition);
            
            definition = null;
            return false;
        }

        public static IEnumerable<TileDefinition> AllTiles
        {
            get
            {
                EnsureLoaded();
                if (_tiles != null)
                    return _tiles.Values;
                
                return new List<TileDefinition>();
            }
        }
    }
}
