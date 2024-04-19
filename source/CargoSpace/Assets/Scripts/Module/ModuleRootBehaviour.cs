using System;
using Bus;
using Logging;
using Module;
using UnityEngine;

public class ModuleRootBehaviour : MonoBehaviour, IModuleBusSource
{
    public readonly Lazy<CgsBus> LazyBus = new Lazy<CgsBus>(LazyFactory);
    Lazy<CgsBus> IModuleBusSource.LazyBus => LazyBus;
    private static CgsBus LazyFactory()
    {
        return new CgsBus(LogManager.GetLogger().Bus);
    }
}