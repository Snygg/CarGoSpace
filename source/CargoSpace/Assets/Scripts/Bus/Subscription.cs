using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Subscription : IDisposable
{
    public Func<Dictionary<string, string>, Task> Callback { get; }
    private Action OnDisposed { get; }

    public Subscription(Func<Dictionary<string, string>, Task> callback, Action onDisposed)
    {
        Callback = callback;
        OnDisposed = onDisposed;
    }

    public void Dispose()
    {
        OnDisposed?.Invoke();
    }
}
