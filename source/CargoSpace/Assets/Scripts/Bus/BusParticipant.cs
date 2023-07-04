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
        public GameObject BusObject;
        private BusBehavior _bus;
        private List<IDisposable> _subscriptions = new List<IDisposable>();
        private LogBehavior _busLogger;

        // Awake is called before Start
        void Awake()
        {
            _busLogger = LogManager.Initialize();
            if (BusObject == null)
            {
                _busLogger.System.LogWarning($"{nameof(BusObject)} is null. Maybe you forgot to assign it.");
            }
            _bus = BusObject.GetComponent<BusBehavior>();
            InitializeSubscriptions();
        }
    
        // Start is called before the first frame update
        void Start()
        {
        
        }

        /// <summary>
        /// Override this when subscribing in Start() is too late in the lifetime of a monobehavior. This will be
        /// called immediately after Awake() on the base class
        /// </summary>
        protected virtual void InitializeSubscriptions() { }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnDestroy()
        {
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
        }

        protected void Publish(string topic, Dictionary<string, string> body)
        {
            if (_bus == null)
            {
                _busLogger.Bus.LogError(new ArgumentException($"{nameof(_bus)} is null. Cannot publish"));
            }
            _bus.Publish(topic, body);
        }

        protected IDisposable Subscribe(string topic, Func<Dictionary<string, string>, Task> handler)
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