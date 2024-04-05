using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Logging;

namespace Bus
{
    public sealed class CgsBus : IDisposable
    {
        private readonly Dictionary<string, HashSet<Subscription>> _subscriptions;
        public static readonly IReadOnlyDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();
        private readonly CgsLogger _logger;

        public CgsBus(CgsLogger logger)
        {
            _logger = logger;
            _subscriptions = new Dictionary<string, HashSet<Subscription>>();
        }

        private void Publish(
            string topic, 
            IReadOnlyDictionary<string, string> body = null,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogError(
                    exception: new ArgumentException("topic cannot be blank", nameof(topic)),
                    context: context,
                    callerMemberName: callerMemberName,
                    callerLineNumber: callerLineNumber,
                    callerFilePath: callerFilePath);
                return;
            }
            if (!_subscriptions.TryGetValue(topic, out var sublist))
            {
                _logger.LogInformation(
                    $"topic has no subscribers: {topic}",
                    context: context,
                    callerMemberName: callerMemberName,
                    callerLineNumber: callerLineNumber,
                    callerFilePath: callerFilePath);
                return;
            }

            foreach(var sub in sublist)
            {
                sub.Callback(body ?? EmptyDictionary).Wait();
            }
        }

        public void Publish(
            BusTopic topic,
            IReadOnlyDictionary<string, string> body = null,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            //todo: check for required params
            Publish(topic.Name,
                body,
                context: context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
        }

        private IDisposable Subscribe(
            string topic, 
            Func<IReadOnlyDictionary<string,string>, Task> callback,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
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
        
            if (!_subscriptions.TryGetValue(topic, out var sublist))
            {
                sublist = new HashSet<Subscription>();
                _subscriptions.Add(topic, sublist);
            }

            _logger.LogVerbose(
                $"Subscribe:{topic}",
                context: context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
            sublist.Add(sub);

            return sub;
        }

        public IDisposable Subscribe(
            BusTopic topic,
            Func<IReadOnlyDictionary<string, string>, Task> callback,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            return Subscribe(topic.Name,
                callback,
                context: context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
        }

        private void Unsubscribe(
            string topic, 
            Subscription sb, 
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (!_subscriptions.TryGetValue(topic, out var sublist))
            {
                return;
            }
            _logger.LogVerbose(
                $"UnSubscribe:{topic}",
                context: context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);

            sublist.Remove(sb);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_subscriptions == null)
            {
                return;
            }

            foreach (var kvp in _subscriptions.ToArray())
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}