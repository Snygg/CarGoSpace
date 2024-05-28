using System;
using System.Collections.Generic;
using System.Globalization;
using Logging;
using R3;
using Scene;
using UnityEngine;
using Weapons;

namespace Ship
{
    public class AutoLazerBehavior : SceneBusParticipant, IControllableWeapon, IGroupableWeapon
    {
        public SerializableReactiveProperty<string> WeaponGroup = new(string.Empty);
        public SerializableReactiveProperty<float> SecondsBetweenShots = new(-1);
        public SerializableReactiveProperty<Vector2> TurretOffset = new();
        ReadOnlyReactiveProperty<string> IGroupableWeapon.GroupName => WeaponGroup;
        private LogBehavior _logger;

        protected override void Awoke()
        {
            _logger = LogManager.GetLogger();

            var disposables = Disposable.CreateBuilder();

            AutoFiring
                .Where(autoFiring => autoFiring)
                .CombineLatest(SecondsBetweenShots.Where(s=> s>=0), (b,f)=>f)
                .Select(intervalSeconds => Observable
                    .Interval(TimeSpan.FromSeconds(intervalSeconds), destroyCancellationToken)
                    .TakeUntil(AutoFiring.Where(d => !d)))
                .Switch()
                .Subscribe(_=>OnFire())
                .AddTo(ref disposables);

            disposables.RegisterTo(destroyCancellationToken);
        }

        private void OnFire()
        {
            _logger.Player.LogWarning("Weapon Group:{0}",this ,values: new System.Object[]{WeaponGroup});
            
            Publish(SceneEvents.TurretFired, new Dictionary<string, string>
            {
                { "source", TurretOffset.CurrentValue.ToString() },
                { "type", "laser" },
                { "strength", 9000.1f.ToString(CultureInfo.InvariantCulture) }
            });
        }

        public SerializableReactiveProperty<bool> AutoFiring { get; } = new(false);
        ReadOnlyReactiveProperty<bool> IControllableWeapon.AutoFiring => AutoFiring;

        public void SetAutoFire(bool doFire)
        {
            AutoFiring.Value = doFire;
        }
    }
}
