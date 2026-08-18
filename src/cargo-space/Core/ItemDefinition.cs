namespace CargoSpace.Core;

public class ItemDefinition
{
    public string StringId { get; set; }
    public string Name { get; set; }
    public float Weight { get; set; }
    public bool Stackable { get; set; }
    public int MaxStack { get; set; }
}
