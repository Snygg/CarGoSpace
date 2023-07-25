using UnityEngine;

namespace Module
{
    public class AttachTargetBehaviour : MonoBehaviour, IAttachTarget
    {
        void Awake()
        {
            if (!AttachTargetTransform)
            {
                AttachTargetTransform = transform;
            }
        
            if (!AttachTargetRigidbody)
            {
                AttachTargetRigidbody = gameObject.GetComponent<Rigidbody2D>();
            }
        }

        Rigidbody2D IAttachTarget.AttachTargetRigidbody { get=>AttachTargetRigidbody; set=>AttachTargetRigidbody=value; }
        Transform IAttachTarget.AttachTargetTransform { get=>AttachTargetTransform; set=>AttachTargetTransform=value; }
        public Rigidbody2D AttachTargetRigidbody;
        public Transform AttachTargetTransform;
    }
}