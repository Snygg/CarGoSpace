using System;

namespace CargoSpace.Core;

[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class GuidIdAttribute : Attribute
{
    public string Prefix { get; }
    public GuidIdAttribute(string prefix) => Prefix = prefix;
}
