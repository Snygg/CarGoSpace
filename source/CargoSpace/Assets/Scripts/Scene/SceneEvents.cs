using System;
using System.Collections.Generic;
using Bus;
using UnityEngine;

namespace Scene
{
    public static class SceneEvents
    {
        public static readonly BusTopic ToggleWeaponGroup = new BusTopic("toggleWeaponGroup",
            new Dictionary<string, Type>
            {
                {"group", typeof(string)}
            });

        public static readonly BusTopic NpcCreate = new BusTopic("npcCreate", new Dictionary<string, Type>
        {
            {"location", typeof(Vector3)}
        });
        public static readonly BusTopic NpcCommand = new BusTopic("npcCommand", new Dictionary<string, Type>
        {
            {"command", typeof(string)}
        });
        public static readonly BusTopic PlayerTargetChanged = new BusTopic("playerTargetChanged", new Dictionary<string, Type>
        {
            {"targetId", typeof(Guid)}
        });
        [Obsolete("This will be remove in future versions")]
        public static readonly BusTopic KeyPressed = new BusTopic("keyPressed", new Dictionary<string, Type>
        {
            {"key", typeof(string)}
        });
        public static readonly BusTopic Input = new BusTopic("Input", new Dictionary<string, Type>
        {
            {"vect", typeof(Vector2)}
        });
        public static readonly BusTopic PlayerTransform = new BusTopic("PlayerTransform", new Dictionary<string, Type>
        {
            {"position", typeof(Vector3)}
        });
    }
}