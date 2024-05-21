using Logging;
using R3;
using UnityEngine;
using Weapons;

namespace Ship
{
    public class AutoLazerBehavior : MonoBehaviour, IControllableWeapon, IGroupableWeapon
    {
        public SerializableReactiveProperty<string> WeaponGroup = new(string.Empty);
        ReadOnlyReactiveProperty<string> IGroupableWeapon.GroupName => WeaponGroup;
        private LogBehavior _logger;

        private void Awake()
        {
            _logger = LogManager.GetLogger();
        }

        public bool AutoFiring { get; private set; }

        public void SetAutoFire(bool doFire)
        {
            AutoFiring = doFire;
            _logger.Player.LogWarning("Weapon Group:{0} doFire:{1}",this ,values: new System.Object[]{WeaponGroup, doFire});
        }
    }
}
