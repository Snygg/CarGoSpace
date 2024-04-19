using UnityEngine;

namespace Module
{
    public interface IAttachTarget
    {
        Rigidbody2D AttachTargetRigidbody { get; }
        Transform AttachTargetTransform { get; }
    }
}