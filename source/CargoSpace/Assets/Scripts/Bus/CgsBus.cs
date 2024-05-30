using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Logging;
using R3;

namespace Bus
{
    public sealed class CgsBus : IDisposable
    {
        private readonly Dictionary<string, BusTopic<IReadOnlyDictionary<string,string>>> _topics;
        public static readonly IReadOnlyDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();
        private readonly CgsLogger _logger;

        public CgsBus(CgsLogger logger)
        {
            _logger = logger;
            _topics = new Dictionary<string, BusTopic<IReadOnlyDictionary<string,string>>>();
        }

        public void Publish(
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
            if (!_topics.TryGetValue(topic, out var busObj))
            {
                _logger.LogInformation(
                    $"topic has no subscribers: {topic}",
                    context: context,
                    callerMemberName: callerMemberName,
                    callerLineNumber: callerLineNumber,
                    callerFilePath: callerFilePath);
                return;
            }

            busObj.Subject.OnNext(body ?? EmptyDictionary);
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

        public Observable<IReadOnlyDictionary<string,string>> Subscribe(
            BusTopic topic,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            return Subscribe(topic.Name, context, callerMemberName, callerLineNumber, callerFilePath);
        }
        public Observable<IReadOnlyDictionary<string, string>> Subscribe(
            string topic,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("topic cannot be blank", nameof(topic));
            }
            if (!_topics.TryGetValue(topic, out var t))
            {
                var subject = new Subject<IReadOnlyDictionary<string, string>>();
                t = new BusTopic<IReadOnlyDictionary<string, string>>
                {
                    Subject = subject,
                    Observable = subject.AsObservable(),
                };
                _topics.Add(topic, t);
            }
            _logger.LogVerbose(
                $"Subscribe:{topic}",
                context: context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
            return t.Observable
                .SubscribeOnThreadPool();
        }
        
        public IDisposable Subscribe(
            string topic, 
            Action<IReadOnlyDictionary<string,string>> callback,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            var t = Subscribe(topic, context, callerMemberName, callerLineNumber, callerFilePath);
            
            return t
                .Subscribe(callback,
                    ex=>OnError(ex,context, callerMemberName, callerLineNumber, callerFilePath), 
                    r=>OnCompleted(r,context, callerMemberName, callerLineNumber, callerFilePath));
        }

        private void OnError(Exception ex,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            try
            {
                _logger.LogError(ex, context, callerMemberName, callerLineNumber, callerFilePath);
            }
            catch
            {
                //if we error while logging we give up
            }
        }

        private void OnCompleted(Result result,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            //intentionally blank
        }

        public IDisposable Subscribe(
            BusTopic topic,
            Action<IReadOnlyDictionary<string, string>> callback,
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

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_topics == null)
            {
                return;
            }

            foreach (var kvp in _topics.ToArray())
            {
                if (kvp.Value == null)
                {
                    continue;
                }
                kvp.Value.Subject.OnCompleted();
            }
        }

        public void Dispose()
        {
            _topics.Clear();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private sealed class BusTopic<T>
        {
            public Observable<T> Observable { get; init; }
            public Subject<T> Subject { get; init; } 
        }
    }
}