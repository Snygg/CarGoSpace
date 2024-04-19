using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bus
{
    public class Subscription : IDisposable
    {
        public Func<IReadOnlyDictionary<string, string>, Task> Callback { get; }
        private Action OnDisposed { get; }

        public Subscription(Func<IReadOnlyDictionary<string, string>, Task> callback, Action onDisposed)
        {
            Callback = callback;
            OnDisposed = onDisposed;
        }

        public void Dispose()
        {
            OnDisposed?.Invoke();
        }
    }
}
