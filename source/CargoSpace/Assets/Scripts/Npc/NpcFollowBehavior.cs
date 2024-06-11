using Module;
using UnityEngine;

namespace Npc
{
    public class NpcFollowBehavior : MonoBehaviour
    {
        public Transform target;
        public float minFollowDistance = 2;

        private IThrusterProvider _thrusterProvider;

        void Awake()
        {
            _thrusterProvider = GetComponent<IThrusterProvider>();
        }

        void FixedUpdate()
        {
            if (!target)
            {
                return;
            }
            var curDistance = Vector3.Distance(transform.position, target.position);
            if (curDistance <= minFollowDistance)
            {
                return;
            }

            var current = (Vector2)transform.position;
            var path = new Vector2(target.position.x, target.position.y) - current;
            var normalized = path.normalized;
            foreach (var thruster in _thrusterProvider.GetThrusters())
            {
                thruster.DirectThrust(normalized);    
            }
        }
    }
}