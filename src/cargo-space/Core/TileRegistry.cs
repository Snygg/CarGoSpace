using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CargoSpace.Core
{
    public enum LayerType
    {
        Floor,
        Surface
    }

    public class LayerTypeJsonConverter : JsonConverter<LayerType>
    {
        public override LayerType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"LayerType must be a string; found {reader.TokenType}.");
            }

            string value = reader.GetString();
            if (value != null &&
                Enum.TryParse<LayerType>(value, out var result) &&
                Enum.IsDefined(typeof(LayerType), result) &&
                result.ToString() == value)
            {
                return result;
            }

            throw new JsonException($"Invalid LayerType '{value}'. Expected 'Floor' or 'Surface'.");
        }

        public override void Write(Utf8JsonWriter writer, LayerType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class TileDefinition
    {
        public byte TypeId { get; set; }
        public string StringId { get; set; }
        public string Name { get; set; }
        public HashSet<string> Tags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> Stats { get; set; } = new();
        public Dictionary<string, float> Attributes { get; set; } = new();
        public Dictionary<string, int> Recipe { get; set; } = new();
        [JsonConverter(typeof(LayerTypeJsonConverter))]
        public LayerType Layer { get; set; }
        public Dictionary<string, int> DeconstructYield { get; set; } = new();
        public byte DeconstructInto { get; set; } = 0;
        public string HexColor { get; set; }

        public bool IsWalkable => Tags != null && Tags.Contains("Walkable");
        public bool IsInteractable => Tags != null && Tags.Contains("Interactable");
        public int BuildCost => Recipe?.Values.Sum() ?? 0;

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
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new System.IO.InvalidDataException("Tile registry JSON must be an object.");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _tiles = new Dictionary<byte, TileDefinition>();
            _stringToId = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                string stringId = property.Name;
                JsonElement element = property.Value;

                if (!element.TryGetProperty("Layer", out JsonElement layerElement) || layerElement.ValueKind != JsonValueKind.String)
                {
                    throw new System.IO.InvalidDataException($"Tile '{stringId}' is missing the required 'Layer' property.");
                }

                TileDefinition tileDef;
                try
                {
                    tileDef = JsonSerializer.Deserialize<TileDefinition>(element.GetRawText(), options);
                }
                catch (JsonException ex)
                {
                    throw new System.IO.InvalidDataException($"Tile '{stringId}' has an invalid 'Layer' value. Expected 'Floor' or 'Surface'.", ex);
                }

                if (tileDef == null)
                {
                    throw new System.IO.InvalidDataException($"Tile '{stringId}' could not be deserialized.");
                }

                tileDef.StringId = stringId;
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
