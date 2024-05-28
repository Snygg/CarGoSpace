using Module;
using R3;
using UnityEngine;

namespace Npc
{
    public class NpcModuleBehavior: MonoBehaviour, ITargetable, IModuleRoot, IThrusterProvider, ITransformProvider
    {
        public bool IsAttachable => _connection == null;
        private IModuleConnection _connection;
        private readonly DisposableBag _attachSubscriptions = new();
        private IThruster[] _thrusters;
        
        string ITargetable.TargetId => name;

        GameObject IComponent.gameObject => gameObject;
        public ITransformProvider TransformProvider => this;
        public Transform Transform => transform;

        private void Awake()
        {
            _thrusters = GetComponents<IThruster>();
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

        public bool IsPlayer => false;
        public IThruster[] GetThrusters()
        {
            return _thrusters;
        }
    }
}