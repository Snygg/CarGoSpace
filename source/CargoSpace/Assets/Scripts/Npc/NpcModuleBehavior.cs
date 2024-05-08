using Module;
using R3;
using UnityEngine;

namespace Npc
{
    public class NpcModuleBehavior: MonoBehaviour, ITargetable, IModuleRoot, IThrusterSource, IRigidBodyProvider
    {
        public bool IsAttachable => _connection == null;
        private IModuleConnection _connection;
        private readonly DisposableBag _attachSubscriptions = new();
        private IThruster[] _thrusters;

        GameObject IComponent.gameObject => gameObject;

        Rigidbody2D IRigidBodyProvider.RigidBody => RigidBody ?? (SelfRigidBody ??= GetComponent<Rigidbody2D>());
        public Rigidbody2D RigidBody => RigidBodyProvider.RigidBody;
        public Rigidbody2D SelfRigidBody;
        public IRigidBodyProvider RigidBodyProvider { get; set; }

        private void Awake()
        {
            _thrusters = GetComponents<IThruster>();
            if (SelfRigidBody == null)
            {
                SelfRigidBody = GetComponent<Rigidbody2D>();
            }
            if (RigidBodyProvider == null)
            {
                RigidBodyProvider = this;    
            }
        }

        public bool Attach(IModuleConnection connection)
        {
            _connection = connection;
            _attachSubscriptions.Add(_connection.Attached
                .Where(a=>!a)
                .Take(1)
                .Subscribe(_=>OnConnectionDetached()));
            RigidBodyProvider = connection.Host;
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
            RigidBodyProvider = this;
        }

        public bool IsPlayer => false;
        public IThruster[] GetThrusters()
        {
            return _thrusters;
        }
    }
}