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
        public bool IsWalkable { get; set; }
        public bool IsInteractable { get; set; }
        public string HexColor { get; set; }

        public Color GetColor()
        {
            return Color.FromHtml(HexColor);
        }
    }

    public static class TileRegistry
    {
        private static Dictionary<TileType, TileDefinition> _tiles;

        static TileRegistry()
        {
            Initialize();
        }

        private static void Initialize()
        {
            string json = @"
            {
                ""Space"": {
                    ""Type"": 0,
                    ""Name"": ""Space"",
                    ""IsWalkable"": false,
                    ""IsInteractable"": false,
                    ""HexColor"": ""#000000""
                },
                ""Deck"": {
                    ""Type"": 1,
                    ""Name"": ""Deck"",
                    ""IsWalkable"": true,
                    ""IsInteractable"": false,
                    ""HexColor"": ""#808080""
                },
                ""Console"": {
                    ""Type"": 2,
                    ""Name"": ""Console"",
                    ""IsWalkable"": true,
                    ""IsInteractable"": true,
                    ""HexColor"": ""#FFA500""
                }
            }";

            var parsed = JsonSerializer.Deserialize<Dictionary<string, TileDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _tiles = new Dictionary<TileType, TileDefinition>();
            foreach (var kvp in parsed)
            {
                _tiles[kvp.Value.Type] = kvp.Value;
            }
        }

        public static TileDefinition Get(TileType type)
        {
            if (_tiles.TryGetValue(type, out TileDefinition definition))
                return definition;
            
            return null;
        }

        public static bool TryGet(TileType type, out TileDefinition definition)
        {
            return _tiles.TryGetValue(type, out definition);
        }

        public static IEnumerable<TileDefinition> AllTiles => _tiles.Values;
    }
}
