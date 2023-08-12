using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using UnityEngine;

namespace Bus
{
    public class BusParticipant : MonoBehaviour
    {
        public GameObject LogObject; 
        public GameObject BusObject;
        private BusBehavior _bus;
        private List<IDisposable> _subscriptions = new List<IDisposable>();
        private LogBehavior _busLogger;

        /// Awake is called before Start. Awaking is called before the bus has been initialized
        protected virtual void Awaking()
        {
            //intentionally empty
        }
        
        /// Awake is called before Start
        private void Awake()
        {
            Awaking();
            _busLogger = LogManager.Initialize(LogObject);
            _bus = BusManager.Initialize(BusObject, _busLogger);
            Awoke();
        }

        /// Awake is called before Start. Awoke is called after the bus has been initialized
        protected virtual void Awoke()
        {
            //intentionally empty
        }

        /// <summary>
        /// called when the object is being destroyed, but before the bus has been disposed
        /// </summary>
        protected virtual void OnDestroying()
        {
            //intentionally empty
        }
        
        /// <summary>
        /// called when the object is being destroyed
        /// </summary>
        private void OnDestroy()
        {
            OnDestroying();
            foreach (var subscription in _subscriptions ?? Enumerable.Empty<IDisposable>())
            {
                try
                {
                    subscription.Dispose();
                }
                catch (Exception ex)
                {
                    _busLogger.Bus.LogWarning($"Error disposing susbscription: {ex.ToString()}");
                }
            }
            OnDestroyed();
        }
        
        /// <summary>
        /// called when the object is being destroyed, but after the bus has been disposed
        /// </summary>
        protected virtual void OnDestroyed()
        {
            //intentionally empty
        }

        protected void Publish(string topic, Dictionary<string, string> body)
        {
            if (_bus == null)
            {
                _busLogger.Bus.LogError(new ArgumentException($"{nameof(_bus)} is null. Cannot publish"));
            }
            _bus.Publish(topic, body);
        }

        protected IDisposable Subscribe(string topic, Func<IReadOnlyDictionary<string, string>, Task> handler)
        {
            if (_bus == null)
            {
                _busLogger.Bus.LogError(new ArgumentException($"{nameof(_bus)} is null. Cannot subscribe"));
            }
            return _bus.Subscribe(topic, handler);
        }

        protected void AddLifeTimeSubscription(IDisposable subscription)
        {
            if (subscription == null)
            {
                _busLogger.Bus.LogWarning("cannot add null subscription");
                return;
            }
            _subscriptions.Add(subscription);
        }
    }
}