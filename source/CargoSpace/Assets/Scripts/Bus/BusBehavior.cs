using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using UnityEngine;

namespace Bus
{
    public class BusBehavior : MonoBehaviour
    {
        public GameObject LogObject;
        private readonly Dictionary<string, HashSet<Subscription>> Subscriptions = new Dictionary<string, HashSet<Subscription>>();

        public void Publish(string topic, Dictionary<string, string> body)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.Bus.LogError(new ArgumentException("topic cannot be blank", nameof(topic)), context:this);
                return;
            }
            if (!Subscriptions.TryGetValue(topic, out var sublist))
            {
                _logger.Bus.LogInformation($"topic has no subscribers: {topic}", context:this);
                return;
            }

            foreach(var sub in sublist)
            {
                sub.Callback(body).Wait();
            }
        }

        public IDisposable Subscribe(string topic, Func<Dictionary<string,string>, Task> callback)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("topic cannot be blank", nameof(topic));
            }

            if (callback == null)
            {
                throw new ArgumentException("callback cannnot be null", nameof(callback));
            }
            Subscription sub = null;
            sub = new Subscription(callback, () => Unsubscribe(topic, sub));
        
            if (!Subscriptions.TryGetValue(topic, out var sublist))
            {
                sublist = new HashSet<Subscription>();
                Subscriptions.Add(topic, sublist);
            }

            _logger.Bus.LogVerbose($"Subscribe:{topic}", context:this);
            sublist.Add(sub);

            return sub;
        }

        public static readonly Dictionary<string, string> EmptyDictionary = new Dictionary<string, string>();
        private LogBehavior _logger;

        void Awake()
        {
            _logger = LogManager.Initialize(LogObject);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnDestroy()
        {
            if (Subscriptions != null)
            {
                foreach (var kvp in Subscriptions.ToArray())
                {
                    if (kvp.Value == null)
                    {
                        continue;
                    }
                    foreach (var subscription in kvp.Value.ToArray())
                    {
                        try
                        {
                            Unsubscribe(kvp.Key, subscription);
                        }
                        catch
                        {
                            //todo: log this, probably not critical
                        }
                    }
                }
            }
        }

        void Unsubscribe(string topic, Subscription sb)
        {
            if (!Subscriptions.TryGetValue(topic, out var sublist))
            {
                return;
            }

            sublist.Remove(sb);
        }
    }
}
