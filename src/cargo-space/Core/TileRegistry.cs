using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CargoSpace.Core
{
    public class TileDefinition
    {
        public byte TypeId { get; set; }
        public string StringId { get; set; }
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
        private static Dictionary<byte, TileDefinition> _tiles = new();
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

            _tiles = new Dictionary<byte, TileDefinition>();
            foreach (var kvp in parsed)
            {
                TileDefinition tileDef = kvp.Value;
                tileDef.StringId = kvp.Key;
                _tiles[tileDef.TypeId] = tileDef;
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

        public static TileDefinition Get(byte typeId)
        {
            EnsureLoaded();
            if (_tiles != null && _tiles.TryGetValue(typeId, out TileDefinition definition))
                return definition;
            
            return null;
        }

        public static bool TryGet(byte typeId, out TileDefinition definition)
        {
            EnsureLoaded();
            if (_tiles != null)
                return _tiles.TryGetValue(typeId, out definition);
            
            definition = null;
            return false;
        }

        public static byte GetId(string stringId)
        {
            EnsureLoaded();
            if (_tiles != null)
            {
                foreach (var kvp in _tiles)
                {
                    if (kvp.Value.StringId == stringId)
                    {
                        return kvp.Key;
                    }
                }
            }

            return byte.MaxValue;
        }

        public static void Register(TileDefinition definition)
        {
            if (_tiles == null)
            {
                _tiles = new Dictionary<byte, TileDefinition>();
            }

            _tiles[definition.TypeId] = definition;
            _isLoaded = true;
            GameLogger.Debug($"Tile registered: {definition.TypeId} - {definition.Name}");
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
