using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Logging;
using R3;
using Scene;
using Unity.Mathematics;
using UnityEngine;

namespace Weapons
{
    public class AutoLazerBehavior : SceneBusParticipant, IControllableWeapon, IGroupableWeapon
    {
        public GameObject LaserPrefab;
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
            
            var strength = 9000.1f;
            FireLaser(source, (targetLocation-source), strength);
        }

        public SerializableReactiveProperty<bool> AutoFiring { get; } = new(false);
        ReadOnlyReactiveProperty<bool> IControllableWeapon.AutoFiring => AutoFiring;

        public void SetAutoFire(bool doFire)
        {
            AutoFiring.Value = doFire;
        }
        
        private void FireLaser(Vector2 source, Vector2 targetDirection, float strength)
        {
            var ignoredGameobject = GetAllParentGameObjects();
            List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
            Physics2D.Raycast(source, targetDirection, new ContactFilter2D(), hitResults);
            var raycastHit2D = hitResults.FirstOrDefault(hr =>
                hr.collider &&
                hr.collider.gameObject &&
                !ignoredGameobject.Contains(hr.collider.gameObject) );
            if (raycastHit2D && raycastHit2D.collider.gameObject.TryGetComponent<ITargetable>(out var tgt))
            {
                RenderLazer(source, raycastHit2D.point);
                tgt.OnDamaged(strength);
                _logger.Combat.LogDebug("Player hit target:{0}", context:this, values: new object[]{tgt});
            }
            else
            {
                RenderLazer(source, targetDirection * 100);
                _logger.Combat.LogVerbose("Player fired and missed");
            }
        }

        private GameObject[] GetAllParentGameObjects()
        {
            var result = new HashSet<GameObject>();
            var currentTransform = transform;
            const int numberOfAncestorsToCheck = 4;
            for (int i = 0; i < numberOfAncestorsToCheck; i++)
            {
                var children = currentTransform.GetComponentsInChildren<ITargetable>();
                foreach (var child in children)
                {
                    result.Add(((MonoBehaviour) child).gameObject);
                }
                result.Add(currentTransform.gameObject);
                currentTransform = currentTransform.parent;
                if (currentTransform == null ||
                    currentTransform.gameObject == null || 
                    currentTransform.gameObject == currentTransform.parent?.gameObject)
                {
                    break;
                }
            }
            return result
                .Where(go=>go!= null)
                .ToArray();
        }

        private void RenderLazer(Vector2 startPosition, Vector2 endPosition)
        {
            var go = Instantiate(LaserPrefab, startPosition, quaternion.identity);
            var lineRenderer = go.GetComponentInChildren<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] {startPosition, endPosition});
        }
    }
}
