using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bus;
using UnityEngine;
using Utils;

namespace Module
{
    public abstract class ModuleBusParticipant : MonoBehaviour
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable(); 
        private FixedJoint2D _joint;
        private Lazy<CgsBus> _lazyBus;
        private CgsBus ModuleBus => _lazyBus.Value;

        private void Awake()
        {
            try
            {
                Awaking();
            }
            finally
            {
                _lazyBus = GetModuleBusSource()?.LazyBus;
                Awoke();    
            }
        }

        private IModuleBusSource GetModuleBusSource()
        {
            var currentObject = gameObject;
            const int maxAncestors = 3;
            for (int i = 0; i < maxAncestors; i++)
            {
                if (currentObject.TryGetComponent<IModuleBusSource>(out var moduleBusSource))
                {
                    return moduleBusSource;
                }

                if (!currentObject.transform.parent)
                {
                    return null;
                }

                currentObject = currentObject.transform.parent.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Similar to the <see cref="Awake"/> method, but called before the bus is initialized
        /// </summary>
        protected virtual void Awaking()
        {
            
        }
        
        /// <summary>
        /// Similar to the <see cref="Awake"/> method, but called after the bus has been initialized
        /// </summary>
        protected virtual void Awoke()
        {
            
        }
        
        private void OnDestroy()
        {
            try
            {
                OnDestroying();
            }
            finally
            {
                _disposables.Dispose();
                OnDestroyed();    
            }
        }

        /// <summary>
        /// Similar to <see cref="OnDestroy"/> but called after lifetime module
        /// bus events are disposed
        /// </summary>
        protected virtual void OnDestroyed()
        {
            
        }

        /// <summary>
        /// Similar to <see cref="OnDestroy"/> but called before lifetime module
        /// bus events are disposed
        /// </summary>
        protected virtual void OnDestroying()
        {
            
        }

        protected void Publish(
            BusTopic topic, 
            IReadOnlyDictionary<string, string> body = null,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            ModuleBus.Publish(topic, 
                body ?? CgsBus.EmptyDictionary,
                context: context ?? this,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath:callerFilePath);
        }

        public delegate void TopicHandler(IReadOnlyDictionary<string, string> body);
        protected IDisposable Subscribe(
            BusTopic topic, 
            TopicHandler handler,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (handler == null)
            {
                throw new ArgumentException("Handler cannot be null", nameof(handler));
            }
            Task Callback(IReadOnlyDictionary<string, string> b)
            {
                handler(b);
                return Task.CompletedTask;
            }

            return ModuleBus.Subscribe(
                topic,
                Callback,
                context:context ?? this,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
        }

        protected void AddLifeTimeSubscription(
            BusTopic busTopic,
            TopicHandler handler,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            AddLifeTimeSubscription(Subscribe(
                busTopic, 
                handler,
                context:context,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath));
        }

        protected void AddLifeTimeSubscription(
            IDisposable subscription,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (subscription == null)
            {
                return;
            }
            _disposables.Add(subscription);
        }
    }
}
