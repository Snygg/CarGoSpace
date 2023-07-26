using UnityEngine;

namespace Module
{
    public interface IAttachTarget
    {
        Rigidbody2D AttachTargetRigidbody { get; set; }
        Transform AttachTargetTransform { get; set; }
    }
}