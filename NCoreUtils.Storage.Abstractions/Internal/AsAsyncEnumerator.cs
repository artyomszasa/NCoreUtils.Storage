using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.Internal
{
    public sealed class AsAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        readonly IEnumerator<T> _source;

        public AsAsyncEnumerator(IEnumerator<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public T Current => _source.Current;

        public ValueTask DisposeAsync()
        {
            _source.Dispose();
            return default;
        }

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_source.MoveNext());
    }
}