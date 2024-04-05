using System;
using Bus;
using Logging;
using UnityEngine;

namespace Scene
{
    public class SceneBusRoot : MonoBehaviour
    {
        public readonly Lazy<CgsBus> LazyBus = new Lazy<CgsBus>(LazyFactory);
        private static CgsBus LazyFactory()
        {
            return new CgsBus(LogManager.GetLogger().Bus);
        }
    }
}
