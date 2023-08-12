using System;
using System.Collections.Generic;

namespace Utils
{
    internal sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        public void Add(IDisposable disposable)
        {
            if (disposable == null)
            {
                return;
            }
            _disposables.Add(disposable);
        }
        public void AddRange(params IDisposable[] disposables)
        {
            _disposables.AddRange(disposables);
        }
        public void AddRange(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
            {
                return;
            }
            _disposables.AddRange(disposables);
        }

        public static CompositeDisposable Combine(CompositeDisposable one, CompositeDisposable two)
        {
            var result = new CompositeDisposable();
            if (one != null)
            {
                result.AddRange(one._disposables);
            }
            if (two != null)
            {
                result.AddRange(two ._disposables);
            }

            return result;
        }
        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                if (disposable == null)
                {
                    continue;
                }

                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}