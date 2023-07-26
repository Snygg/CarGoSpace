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
    }
}