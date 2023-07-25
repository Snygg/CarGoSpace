using System;
using Bus;

public interface IModuleBusSource
{
    Lazy<CgsBus> LazyBus { get; }
}