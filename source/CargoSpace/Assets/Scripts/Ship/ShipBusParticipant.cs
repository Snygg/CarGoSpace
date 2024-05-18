using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bus;
using Logging;
using Player;
using R3;
using UnityEngine;

namespace Ship
{
    [RequireComponent(typeof(IShipBusProvider))]
    public abstract class ShipBusParticipant : MonoBehaviour
    {
        private readonly DisposableBag _disposables = new();
        private IShipBusProvider _shipBusProvider;
        private LogBehavior _logger;
        private CgsBus Bus => _shipBusProvider.Bus;
        
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
                _logger = LogManager.GetLogger();
                _shipBusProvider = GetComponent<IShipBusProvider>();
                if (_shipBusProvider == null)
                {
                    _logger.Bus.LogWarning("Cannot find ship bus", this);
                }
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
            Bus.Publish(topic, 
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
                _logger.Bus.LogError("Handler cannot be null", this);
            }
            void Callback(IReadOnlyDictionary<string, string> a) => handler(a);

            return Bus.Subscribe(
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

        private void AddLifeTimeSubscription(
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