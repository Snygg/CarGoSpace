using R3;
using UnityEngine;

namespace Weapons
{
    public interface IControllableWeapon
    {
        ReadOnlyReactiveProperty<bool> AutoFiring { get; }
        void SetAutoFire(bool doFire);
        GameObject gameObject { get ; } 
    }
}