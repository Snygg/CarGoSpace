using R3;
using UnityEngine;

namespace Npc
{
    public class NpcModuleBehavior: MonoBehaviour, ITargetable, IModuleRoot
    {
        public SerializableReactiveProperty<bool> IsAttached { get; private set; } = new SerializableReactiveProperty<bool>(false);
        private IModuleConnection _connection;
        private readonly DisposableBag _attachSubscriptions = new();

        GameObject IComponent.gameobject => gameObject;

        public void Attach(IModuleConnection connection)
        {
            _connection = connection;
            _attachSubscriptions.Add(_connection.Hp.Subscribe(OnConnectionHpChange));
            IsAttached.Value = true;
        }

        private void OnConnectionHpChange(float value)
        {
            if (_connection.Hp.Value <= 0)
            {
                Detach();
            }
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
            _connection?.Detach();
            _attachSubscriptions.Clear();
            _connection = null;
            IsAttached.Value = false;
        }

        public bool IsPlayer => false;
    }
}