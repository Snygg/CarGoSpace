using System;
using Bus;
using Logging;
using UnityEngine;

namespace Scene
{
    public class SceneBusRoot : MonoBehaviour, ISceneBusProvider
    {
        public readonly Lazy<CgsBus> LazyBus = new Lazy<CgsBus>(LazyFactory);
        CgsBus ISceneBusProvider.Bus => LazyBus.Value; 
        private static CgsBus LazyFactory()
        {
            return new CgsBus(LogManager.GetLogger().Bus);
        }

        private void OnDestroy()
        {
            LazyBus.Value.Dispose();
        }
    }
}
