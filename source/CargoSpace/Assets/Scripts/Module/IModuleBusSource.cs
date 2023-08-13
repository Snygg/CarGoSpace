using System;
using Bus;

namespace Module
{
    public interface IModuleBusSource
    {
        Lazy<CgsBus> LazyBus { get; }
    }
}