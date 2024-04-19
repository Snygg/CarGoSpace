using System.Collections.Generic;
using Npc;
using R3;
using UnityEngine;

namespace Module
{
    /// <summary>
    /// The behavior for the game object which serves as the parent for zero to many modules
    /// </summary>
    public class ModuleHostBehaviour : MonoBehaviour, IModuleHost
    {
        private CompositeThruster _thrusters = new();
        private Dictionary<IModuleRoot, DisposableBag> _moduleSubscriptions = new();

        public void AddModule(IModuleRoot module, IModuleConnection connection)
        {
            if (module.gameobject.TryGetComponent<IThrusterSource>(out var thrusterSource))
            {
                _thrusters.Add(thrusterSource);    
            }

            var subscriptions = new DisposableBag();
            subscriptions.Add(connection.Hp
                .Where(hp=>hp<=0)
                .Take(1)
                .Subscribe(_=>OnConnectionDestroyed(module, connection)));
            _moduleSubscriptions.Add(module, subscriptions);
        }

        private void OnConnectionDestroyed(IModuleRoot module, IModuleConnection connection)
        {
            if (_moduleSubscriptions.Remove(module, out var subscriptions))
            {
                subscriptions.Dispose();
            }
        }
    }
}