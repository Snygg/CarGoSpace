using UnityEngine;

namespace Weapons
{
    public class SelfDestructFrameCountBehavior : MonoBehaviour
    {
        public int FramesRemaining = 0;

        public GameObject ToDestroy;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (FramesRemaining > 0)
            {
                FramesRemaining--;
                return;
            }

            if (ToDestroy)
            {
                Destroy(ToDestroy);    
            }
        }
    }
}
