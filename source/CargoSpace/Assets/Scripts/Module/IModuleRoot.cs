using Npc;
using R3;
using UnityEngine;

namespace Module
{
    /// <summary>
    /// Represents the top of a module's (transform/scene) hierarchy
    /// </summary>
    public interface IModuleRoot: IComponent, IAttachable
    {
        /// <summary>
        /// May be null. The thrusterprovuder if this module provides a thruster
        /// </summary>
        IThrusterProvider ThrusterProvider { get; }

        Transform Transform { get; }
    }
}