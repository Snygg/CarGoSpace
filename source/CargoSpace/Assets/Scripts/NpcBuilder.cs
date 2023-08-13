using Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    internal class NpcBuilder : MonoBehaviour
    {
        public GameObject LogObject;
        public GameObject BusObject;
        public GameObject Hull; 
        public GameObject Engine;
        public GameObject Laser;
        

        public GameObject BuildNpc(Vector3 location) 
        {
            

            GameObject go = Instantiate<GameObject>(Hull, location, new Quaternion());
            var follow = go.GetComponent<NpcFollowPlayerBehavior>();
            //follow.DirectorObject = DirectorObject;

            var targetable = go.GetComponentsInChildren<TargetableBehavior>(includeInactive: false);
            foreach (var targetableBehavior in targetable)
            {
                targetableBehavior.LogObject = LogObject;
                targetableBehavior.BusObject = BusObject;
                targetableBehavior.Module = targetableBehavior.gameObject;
            }

            //todo: remove them from the list when they get destroyed
            return go;
        }
    }
}
