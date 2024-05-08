using System.Collections.Generic;
using Npc;
using R3;
using UnityEngine;

namespace Module
{
    /// <summary>
    /// The behavior for the game object which serves as the parent for zero to many modules
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ModuleHostBehaviour : MonoBehaviour, IModuleHost, IThrusterProvider
    {
        private CompositeThruster _thrusters = new();
        private Dictionary<IModuleRoot, DisposableBag> _moduleSubscriptions = new();
        public bool IsAttachable => true;
        public Rigidbody2D SelfRigidBody { get; private set; }
        Rigidbody2D IRigidBodyProvider.RigidBody =>
            RigidBodyProvider == null || this.Equals(RigidBodyProvider)
                ? SelfRigidBody ??= GetComponent<Rigidbody2D>()
                : RigidBodyProvider.RigidBody;

        public IRigidBodyProvider RigidBodyProvider { get; set; }

        private void Awake()
        {
            if (SelfRigidBody == null)
            {
                SelfRigidBody = GetComponent<Rigidbody2D>();    
            }
            if (RigidBodyProvider == null)
            {
                RigidBodyProvider = this;    
            }
            _thrusters.RigidBodyProvider = this;
            thrusters = new []{_thrusters};
        }
        
        public bool Attach(IModuleConnection connection)
        {
            if (connection.Module.gameObject.TryGetComponent<IThrusterProvider>(out var thrusterSource))
            {
                _thrusters.Add(thrusterSource);    
            }

            if (!_moduleSubscriptions.ContainsKey(connection.Module))
            {
                _moduleSubscriptions[connection.Module] = new DisposableBag();
            }
            _moduleSubscriptions[connection.Module].Add(connection.Attached
                .Where(a=>!a)
                .Take(1)
                .Subscribe(_=>OnConnectionDestroyed(connection.Module, connection)));
            return true;
        }

        private void OnConnectionDestroyed(IModuleRoot module, IModuleConnection connection)
        {
            if (_moduleSubscriptions.Remove(module, out var subscriptions))
            {
                subscriptions.Dispose();
            }
        }

        private IThruster[] thrusters;
        public IThruster[] GetThrusters()
        {
            return thrusters;
        }
    }
}