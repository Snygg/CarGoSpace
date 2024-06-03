/// <summary>
/// what a weapon knows when deciding what to target
/// </summary>
public interface ITargetable
{
    void OnDamaged(float strength);
    /// <summary>
    ///  Id or null if this is not targetable by player weapons
    /// </summary>
    string TargetId { get; }
    ITransformProvider TransformProvider { get; }
}