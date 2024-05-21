using System.Collections.Generic;
using Bus;
using Logging;
using R3;
using Scene;
using UnityEngine;
using Weapons;

namespace Player
{
    /// <summary>
    /// The place to subscribe to scene bus events and turn them in to ship-wide behavior. There should only be one of these components per ship.
    /// </summary>
    public class PlayerShipRouter : MonoBehaviour
    {
        private CgsBus _sceneBus;
        private LogBehavior _logger;

        void Awake()
        {
            _sceneBus = FindObjectOfType<SceneBusRoot>().LazyBus.Value;
            _logger = LogManager.GetLogger();
            
            SceneSubscribe();
        }

        private void SceneSubscribe()
        {
            var sceneSubscriptions = Disposable.CreateBuilder();
            
            _sceneBus
                .Subscribe(SceneEvents.ToggleWeaponGroup, OnToggleWeaponGroup, this)
                .AddTo(ref sceneSubscriptions);
            
            //this line builds the disposable builder, all subscriptions must occur before this line
            sceneSubscriptions.RegisterTo(destroyCancellationToken);
        }

        private void OnToggleWeaponGroup(IReadOnlyDictionary<string, string> body)
        {
            if (!body.TryGetValue("group", out var groupName))
            {
                return;
            }

            foreach (var module in GetComponentsInChildren<IWeaponModule>())
            {
                module.ToggleWeaponGroup(groupName);   
            }   
        }
    }
}