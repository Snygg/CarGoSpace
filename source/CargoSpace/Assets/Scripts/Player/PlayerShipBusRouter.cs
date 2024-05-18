using Bus;
using Scene;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// The place to subscribe to scene bus events and turn them in to ship-wide events. Alternatively the place to subscribe to
    /// ship events and publish as scene events. There should only be one of these components per ship.
    /// </summary>
    [RequireComponent(typeof(IShipBusProvider))]
    public class PlayerShipBusRouter : MonoBehaviour
    {
        private IShipBusProvider _shipBusProvider;
        private CgsBus _sceneBus;

        void Awake()
        {
            _shipBusProvider = GetComponent<IShipBusProvider>();
            _sceneBus = FindObjectOfType<SceneBusRoot>().LazyBus.Value;
        }
    }
}