using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module
{
    
    public class AttachableBehavior : ModuleBusParticipant
    {
        public FixedJoint2D Joint;
        public Transform ParentTransform;
        public Rigidbody2D ParentBody;
        public bool Attached { get; private set; }
        
        protected override void Awoke()
        {
            AddLifeTimeSubscription(ModuleEvents.Attached,OnAttached);
            AddLifeTimeSubscription(ModuleEvents.Detached,OnDetached);
        }

        void Start()
        {
            //attach on start because it fires after awake, 
            // and it only fires once
            AttachModule(ParentBody, ParentTransform);
        }

        /// <summary>
        /// Attach this module to the associated gameobject (requires a rigidbody)
        /// </summary>
        public void AttachModule(IAttachTarget target)
        {
            if (target == null)
            {
                return;
            }
            AttachModule(target.AttachTargetRigidbody, target.AttachTargetTransform);
        }
        private void AttachModule(Rigidbody2D hull, Transform parent)
        {
            //already attached
            if (Joint)
            {
                Attached = true;
                return;
            }
            
            if (!hull)
            {
                return;
            }

            if (!parent)
            {
                return;
            }

            ParentTransform = parent;
            ParentBody = hull;

            Joint = gameObject.AddComponent<FixedJoint2D>();
            Joint.connectedBody = hull;
            
            //this variable fixes a compiler warning
            var innerTransform = transform;
            
            Joint.connectedAnchor = innerTransform.position;
            Joint.dampingRatio = 1;
            Joint.breakForce = Single.PositiveInfinity;
            Joint.breakTorque = Single.PositiveInfinity;
            
            innerTransform.parent = parent;

            Publish(ModuleEvents.Attached, context:this);
            
        }

        private void OnAttached(IReadOnlyDictionary<string, string> _)
        {
            Attached = true;
        }

        private void OnDetached(IReadOnlyDictionary<string, string> _)
        {
            Attached = false;
            
            Destroy(Joint);
            Joint = null;
        }
    }
}
