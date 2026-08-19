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
        public HashSet<string> Tags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> Stats { get; set; } = new();
        public Dictionary<string, float> Attributes { get; set; } = new();
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
        private static Dictionary<string, byte> _stringToId = new();
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
            _stringToId = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in parsed)
            {
                TileDefinition tileDef = kvp.Value;
                tileDef.StringId = kvp.Key;
                tileDef.Tags = new HashSet<string>(tileDef.Tags ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
                _tiles[tileDef.TypeId] = tileDef;
                _stringToId[tileDef.StringId] = tileDef.TypeId;
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
            if (_stringToId != null && _stringToId.TryGetValue(stringId, out byte id))
            {
                return id;
            }

            GameLogger.Warning($"TileRegistry: Could not find ID for {stringId}");
            return byte.MaxValue;
        }

        public static void Register(TileDefinition definition)
        {
            if (_tiles == null)
            {
                _tiles = new Dictionary<byte, TileDefinition>();
            }

            if (_stringToId == null)
            {
                _stringToId = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            }

            definition.Tags = new HashSet<string>(definition.Tags ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
            _tiles[definition.TypeId] = definition;
            _stringToId[definition.StringId] = definition.TypeId;
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
