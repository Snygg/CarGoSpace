using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CargoSpace.Core
{
    public static class ItemRegistry
    {
        private static Dictionary<string, ItemDefinition> _items = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isLoaded = false;

        public static void LoadFromFile(string filePath)
        {
            if (!FileAccess.FileExists(filePath))
            {
                GameLogger.Error($"Item registry file not found: {filePath}");
                return;
            }

            using FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            LoadFromJson(json);
        }

        public static void LoadFromJson(string json)
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, ItemDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in parsed)
            {
                ItemDefinition itemDef = kvp.Value;
                itemDef.StringId = kvp.Key;
                _items[itemDef.StringId] = itemDef;
            }

            _isLoaded = true;
            GameLogger.Debug($"ItemRegistry loaded {_items.Count} item definitions");
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                GameLogger.Warning("ItemRegistry accessed before loading. Call LoadFromFile() during game startup.");
            }
        }

        public static ItemDefinition Get(string id)
        {
            EnsureLoaded();
            if (_items != null && _items.TryGetValue(id, out ItemDefinition definition))
                return definition;

            GameLogger.Warning($"ItemRegistry: Could not find definition for {id}");
            return null;
        }

        public static bool TryGet(string id, out ItemDefinition definition)
        {
            EnsureLoaded();
            if (_items != null)
                return _items.TryGetValue(id, out definition);

            definition = null;
            return false;
        }

        public static IEnumerable<ItemDefinition> AllItems
        {
            get
            {
                EnsureLoaded();
                if (_items != null)
                    return _items.Values;

                return new List<ItemDefinition>();
            }
        }
    }
}
