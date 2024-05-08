using Module;
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
        /// <summary>
        /// Setting this to false will destroy the connection. Once detached, a new connection must be created - this connection cannot be reused
        /// </summary>
        SerializableReactiveProperty<bool> Attached { get; }
        
        /// <summary>
        /// The thing to which a module attaches
        /// </summary>
        /// <remarks>undefined if Disposed is true</remarks>
        IModuleHost Host { get; }
        
        /// <summary>
        /// The module which is attached
        /// </summary>
        /// <remarks>undefined if Disposed is true</remarks>
        IModuleRoot Module { get; }
    }
}