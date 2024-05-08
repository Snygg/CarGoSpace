using Npc;

namespace Module
{
    public interface IAttachable
    {
        /// <summary>
        /// returns true when this object is in a state which can be attached
        /// </summary>
        bool IsAttachable { get; }

        bool Attach(IModuleConnection connection);
    }
}