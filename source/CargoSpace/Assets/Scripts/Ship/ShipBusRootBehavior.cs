using Bus;
using Logging;
using Player;
using UnityEngine;

namespace Ship
{
    public class ShipBusRootBehavior : MonoBehaviour, IShipBusProvider
    {
        private CgsBus _bus;
        public CgsBus Bus => _bus;

        void Awake()
        {
            _bus = new CgsBus(LogManager.GetLogger().Bus);
        }
        private void OnDestroy()
        {
            _bus.Dispose();
        }
    }
}