using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bus;
using R3;
using UnityEngine;

namespace Module
{
    public abstract class ModuleBusParticipant : MonoBehaviour
    {
        private readonly DisposableBag _disposables = new DisposableBag(); 
        private Lazy<CgsBus> _lazyBus;
        private CgsBus ModuleBus => _lazyBus.Value;
        
        protected delegate void TopicHandler(IReadOnlyDictionary<string, string> body);

        /// <summary>
        /// Similar to the <see cref="Awake"/> method, but called before the bus is initialized
        /// </summary>
        protected virtual void Awaking()
        {
            //intentionally empty
        }
        
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
        
        /// <summary>
        /// Similar to the <see cref="Awake"/> method, but called after the bus has been initialized
        /// </summary>
        protected virtual void Awoke()
        {
            //intentionally empty
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
        /// Similar to <see cref="OnDestroy"/> but called before lifetime module
        /// bus events are disposed
        /// </summary>
        protected virtual void OnDestroying()
        {
            //intentionally empty
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
                if (_lazyBus.IsValueCreated)
                {
                    _lazyBus.Value.Dispose();
                }
                OnDestroyed();    
            }
        }

        /// <summary>
        /// Similar to <see cref="OnDestroy"/> but called after lifetime module
        /// bus events are disposed
        /// </summary>
        protected virtual void OnDestroyed()
        {
            //intentionally empty
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

            void Callback(IReadOnlyDictionary<string, string> a) => handler(a);

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
