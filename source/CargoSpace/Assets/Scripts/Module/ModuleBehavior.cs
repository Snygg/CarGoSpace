using System;
using Bus;
using JetBrains.Annotations;
using UnityEngine;
using Utils;

namespace Module
{
    public class ModuleBehavior : BusParticipant, IModule, IDamageable
    {
        private FixedJoint2D _joint;

        /// <summary>
        /// Attach this module to the associated gameobject (requires a rigidbody)
        /// </summary>
        /// <param name="hull"></param>
        public void AttachModule(Rigidbody2D hull, Transform parent)
        {
            if (!hull)
            {
                return;
            }

            //already attached
            if (_joint)
            {
                return;
            }

            _joint = gameObject.AddComponent<FixedJoint2D>();
            _joint.connectedBody = hull;
            _joint.connectedAnchor = transform.position;
            _joint.dampingRatio = 1;
            _joint.breakForce = Single.PositiveInfinity;
            _joint.breakTorque = Single.PositiveInfinity;
            transform.parent = parent;
        }

        public void DetachModule()
        {
            if (!_joint)
            {
                return;
            }
            Destroy(_joint);
            _joint = null;
            transform.parent = null;
        }

        public void OnHpPercentChanged(float hpPercent)
        {
            if (hpPercent <= 0)
            {
                DetachModule();
            }
        }
    }
}
