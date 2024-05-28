public interface ITargetable
{
    void OnDamaged(float strength);
    bool IsPlayer { get; }
    string TargetId { get; }
    ITransformProvider TransformProvider { get; }
}