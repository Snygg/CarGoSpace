using R3;

namespace Npc
{
    /// <summary>
    /// represents the connection between a module and the module host
    /// </summary>
    public interface IModuleConnection
    {
        void Detach();
        void ApplyDamage(float strength);
        SerializableReactiveProperty<float> Hp { get; }
    }
}