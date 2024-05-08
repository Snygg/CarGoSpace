using UnityEngine;

namespace Module
{
    public interface IRigidBodyProvider
    {
        Rigidbody2D RigidBody { get; }
    }
}