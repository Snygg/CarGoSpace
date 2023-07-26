public interface ITargetable
{
    void OnDamaged(float strength);
    bool IsPlayer { get; }
}