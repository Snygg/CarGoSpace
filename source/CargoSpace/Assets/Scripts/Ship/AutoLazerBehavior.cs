using System;
using Logging;
using R3;
using UnityEngine;
using UnityEngine.Serialization;
using Weapons;

namespace Ship
{
    public class AutoLazerBehavior : MonoBehaviour, IControllableWeapon, IGroupableWeapon
    {
        public SerializableReactiveProperty<string> WeaponGroup = new(string.Empty);
        public SerializableReactiveProperty<float> SecondsBetweenShots = new(-1);
        public SerializableReactiveProperty<Vector2> TurretOffset = new();
        ReadOnlyReactiveProperty<string> IGroupableWeapon.GroupName => WeaponGroup;
        private LogBehavior _logger;

        private void Awake()
        {
            _logger = LogManager.GetLogger();

            var disposables = Disposable.CreateBuilder();

            AutoFiring
                .CombineLatest(SecondsBetweenShots, (b,f)=>new Tuple<bool, float>(b,f))
                .Where(tuple => tuple.Item1 && tuple.Item2 >=0)
                .Select(_ => Observable
                    .Interval(TimeSpan.FromSeconds(SecondsBetweenShots.CurrentValue), destroyCancellationToken)
                    .TakeUntil(AutoFiring.Where(d => !d)))
                .Switch()
                .Subscribe(_=>OnFire())
                .AddTo(ref disposables);

            disposables.RegisterTo(destroyCancellationToken);
        }

        private void OnFire()
        {
            _logger.Player.LogWarning("Weapon Group:{0}",this ,values: new System.Object[]{WeaponGroup});
        }

        public SerializableReactiveProperty<bool> AutoFiring { get; } = new(false);
        ReadOnlyReactiveProperty<bool> IControllableWeapon.AutoFiring => AutoFiring;

        public void SetAutoFire(bool doFire)
        {
            AutoFiring.Value = doFire;
        }
    }
}
