using Godot;
using System.Collections.Generic;

namespace CargoSpace.Core
{
    public class Blueprint
    {
        public Vector2I Position;
        public byte TargetTypeId;
        public Dictionary<string, int> Required = new();
        public Dictionary<string, int> Delivered = new();
    }
}
