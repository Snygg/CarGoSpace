using System.Collections.Generic;
using System.Linq;
using Npc;
using R3;
using UnityEngine;

namespace Module
{
    /// <summary>
    /// The behavior for the game object which serves as the parent for zero to many modules
    /// </summary>
    public class ModuleHostBehaviour : MonoBehaviour, IModuleHost, IThrusterProvider
    {
        private CompositeThruster _thrusters = new();
        private Dictionary<IModuleRoot, DisposableBag> _moduleSubscriptions = new();
        public bool IsAttachable => true;
        public Rigidbody2D Rigidbody;
        Rigidbody2D IRigidBodyProvider.RigidBody => Rigidbody ??= GetComponent<Rigidbody2D>();
        public SerializableReactiveProperty<bool> destroyWhenAllModulesDestroyed = new(true);

        private void Awake()
        {
            thrusters = new []{_thrusters};
            if (!Rigidbody)
            {
                Rigidbody = GetComponent<Rigidbody2D>();
            }

            var modules = GetComponentsInChildren<IModuleRoot>();
            var parent = gameObject;
            foreach (var module in modules)
            {
                if (_moduleSubscriptions.ContainsKey(module))
                {
                    continue;
                }
                var child = module.gameObject;
                DirectorBehavior.CreateConnection(parent, child);
            }
        }
        
        public bool Attach(IModuleConnection connection)
        {
            if (connection.Module.ThrusterProvider != null) 
            {
                _thrusters.Add(connection.Module.ThrusterProvider);
            }

            if (!_moduleSubscriptions.ContainsKey(connection.Module))
            {
                _moduleSubscriptions[connection.Module] = new DisposableBag();
            }
            _moduleSubscriptions[connection.Module].Add(connection.Attached
                .Where(a=>!a)
                .Take(1)
                .Subscribe(_=>OnConnectionDestroyed(connection)));
            return true;
        }
        
        private void Detach(IModuleConnection connection)
        {
            if (_moduleSubscriptions.Remove(connection.Module, out var subscriptions))
            {
                subscriptions.Dispose();
                _thrusters.Remove(connection.Module.ThrusterProvider);
            }

            if (destroyWhenAllModulesDestroyed.Value &&
                !_moduleSubscriptions.Any())
            {
                Destroy(gameObject);
            }
        }

        private void OnConnectionDestroyed(IModuleConnection connection)
        {
            Detach(connection);
        }

        private IThruster[] thrusters;
        public IThruster[] GetThrusters()
        {
            return thrusters;
        }
    }
}