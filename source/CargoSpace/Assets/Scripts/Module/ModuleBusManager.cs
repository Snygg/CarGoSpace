using System;
using Bus;
using Logging;

namespace Module
{
    internal static class ModuleBusManager
    {
        private static Lazy<CgsBus> _nextBus = NextBus();
        private static IDisposable _moduleCreationContext = null;

        private static Lazy<CgsBus> NextBus()
        {
            return new Lazy<CgsBus>(() => new CgsBus(GetLogger()));
        }

        private static CgsLogger GetLogger()
        {
            var logger = LogManager.GetLogger();
            return logger.Bus;
        }

        public static Lazy<CgsBus> GetBus()
        {
            var result = _nextBus;
            if (_moduleCreationContext == null)
            {
                _nextBus = NextBus();    
            }
            return result;
        }

        /// <summary>
        /// Surround the instantiation of a module with using  <see cref="ModuleBusManager"/>.<see cref="CreateModuleCreationContext"/>
        /// to make all <see cref="ModuleBusParticipant"/> use a common bus
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IDisposable CreateModuleCreationContext()
        {
            if (_moduleCreationContext != null)
            {
                throw new Exception("Cannot create a new context when one already exists");
            }
            
            _nextBus = NextBus();

            _moduleCreationContext = new ModuleCreationContext(() =>
            {
                _moduleCreationContext = null;
                _nextBus = NextBus();
            });
            return _moduleCreationContext;
        }
        
        private class ModuleCreationContext : IDisposable
        {
            private readonly Action _callback;

            public ModuleCreationContext(Action callback)
            {
                _callback = callback;
            }

            public void Dispose()
            {
                _callback?.Invoke();
            }
        }
    }
}