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

            var playerTargetChanged = Subscribe(SceneEvents.PlayerTargetChanged, this);
            var playerTargetId = playerTargetChanged
                .Select(d => d.TryGetValue("targetId", out var targetId) && !string.IsNullOrWhiteSpace(targetId)
                    ? targetId
                    : null)
                .DistinctUntilChanged();
            var playerTargetable = playerTargetId
                .DistinctUntilChanged()
                .Select(id => LookupServiceManager.GetService().GetTargetableById(id))
                .ToReadOnlyReactiveProperty();
            //var hasTarget = playerTargetable.Select(t=>t!=null)
             //   .DistinctUntilChanged();

            var hasValidInterval = SecondsBetweenShots.Where(s=> s>=0);
            var hasTargetTrue = playerTargetable.Where(h=>h!=null);
            var autoFiringTrue = AutoFiring
                .Where(autoFiring => autoFiring);
            autoFiringTrue
                .CombineLatest(hasValidInterval, (_,f)=>f)
                .CombineLatest(hasTargetTrue,(f,t)=>new Tuple<float, ITargetable>(f,t))
                .Select(tuple =>
                {
                    var autoFiringFalse = AutoFiring
                        .Where(d => !d)
                        .Select(_=>Unit.Default);
                    var hasTargetFalse = playerTargetable
                        .Where(h => h == null)
                        .Select(_=>Unit.Default);
                    var stopInterval = Observable.Amb(
                        autoFiringFalse,
                        hasTargetFalse);
                    return Observable
                        .Interval(TimeSpan.FromSeconds(tuple.Item1), destroyCancellationToken)
                        .TakeUntil(stopInterval)
                        .Select(_=>tuple.Item2);
                })
                .Switch()
                .Subscribe(t=>OnFire(t.TransformProvider.Transform.position))
                .AddTo(ref disposables);

            disposables.RegisterTo(destroyCancellationToken);
        }

        private void OnFire(Vector2 targetLocation)
        {
            var source = (Vector2)gameObject.transform.position + TurretOffset.CurrentValue;
            Publish(SceneEvents.TurretFired, new Dictionary<string, string>
            {
                { "source", source.ToString() },
                {"destination", targetLocation.ToString() }, 
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
