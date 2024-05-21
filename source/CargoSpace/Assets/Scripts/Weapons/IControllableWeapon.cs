using UnityEngine;

namespace Weapons
{
    public interface IControllableWeapon
    {
        bool AutoFiring { get; }
        void SetAutoFire(bool doFire);
        GameObject gameObject { get ; } 
    }
}