using Npc;

namespace Module
{
    /// <summary>
    /// Represents the top of a module's (transform/scene) hierarchy
    /// </summary>
    public interface IModuleRoot: IComponent, IAttachable
    {
        IRigidBodyProvider RigidBodyProvider { get; set; }
    }
}