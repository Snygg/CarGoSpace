using System;
using System.Collections.Generic;
using System.Linq;
using Logging;
using Module;
using Npc;
using R3;
using UnityEngine;
using Weapons;

namespace Player
{
    public class PlayerModuleBehavior: MonoBehaviour, ITargetable, IModuleRoot, IThrusterProvider, IWeaponModule,
        ITransformProvider, IClickable
    {
        public bool IsAttachable => _connection == null;
        private IModuleConnection _connection;
        private readonly DisposableBag _attachSubscriptions = new(); 
        private IThruster[] _thrusters;
        private LogBehavior _logger;
        public ReactiveProperty<IReadOnlyCollection<IControllableWeapon>> WeaponGroup1 { get; } = new(Array.Empty<IControllableWeapon>());
        public ReactiveProperty<IReadOnlyCollection<IControllableWeapon>> WeaponGroup2 { get; } = new(Array.Empty<IControllableWeapon>());
        public ReactiveProperty<IReadOnlyCollection<IControllableWeapon>> WeaponGroup3 { get; } = new(Array.Empty<IControllableWeapon>());

        GameObject IComponent.gameObject => gameObject;
        public string TargetId => null;
        public ITransformProvider TransformProvider => this;
        public Transform Transform => transform;
        public IThrusterProvider ThrusterProvider => this;

        private void Awake()
        {
            _logger = LogManager.GetLogger();
            _thrusters = GetComponents<IThruster>();
            var weaponComponents = GetComponents<IControllableWeapon>();
            var x = weaponComponents.Aggregate(new Dictionary<string, List<IControllableWeapon>>(), (d,w) =>
            {
                var groupableWeapon = w.gameObject.GetComponent<IGroupableWeapon>();
                if (groupableWeapon == null || string.IsNullOrWhiteSpace(groupableWeapon.GroupName.CurrentValue))
                {
                    return d;
                }
                var key = groupableWeapon.GroupName.CurrentValue;
                //todo: subscribe to group name change
                if (!d.ContainsKey(key))
                {
                    d.Add(key, new List<IControllableWeapon>());
                }
                d[key].Add(w);
                return d;
            });
            if (x.TryGetValue("1", out var l1))
            {
                WeaponGroup1.Value = l1;
            }
            if (x.TryGetValue("2", out var l2))
            {
                WeaponGroup2.Value = l2;
            }
            if (x.TryGetValue("3", out var l3))
            {
                WeaponGroup3.Value = l3;
            }
        }

        public bool Attach(IModuleConnection connection)
        {
            _connection = connection;
            _attachSubscriptions.Add(_connection.Attached
                .Where(a=>!a)
                .Take(1)
                .Subscribe(_=>OnConnectionDetached()));
            
            return true;
        }

        private void OnConnectionDetached()
        {
            Detach();
        }

        public void OnDamaged(float strength)
        {
            if (_connection != null)
            {
                _connection.ApplyDamage(strength);
            }
        }

        public void Detach()
        {
            _attachSubscriptions.Clear();
            _connection = null;
        }

        public IThruster[] GetThrusters()
        {
            return _thrusters;
        }

        public void ToggleWeaponGroup(string groupName)
        {
            var group = groupName == "1"
                ? WeaponGroup1.CurrentValue
                : groupName == "2"
                    ? WeaponGroup2.CurrentValue
                    : groupName == "3"
                        ? WeaponGroup3.CurrentValue
                        : Array.Empty<IControllableWeapon>();
            if (!group.Any())
            {
                return;
            }

            var first = group.First();
            var currentState = first.AutoFiring.CurrentValue;
            var newState = !currentState;
            foreach (var weapon in group)
            {
                weapon.SetAutoFire(newState);
            }
        }
    }
}