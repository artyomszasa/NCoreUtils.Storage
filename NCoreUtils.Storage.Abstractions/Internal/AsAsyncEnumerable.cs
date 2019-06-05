using System;
using System.Collections.Generic;
using System.Threading;

namespace NCoreUtils.Storage.Internal
{
    public sealed class AsAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly IEnumerable<T> _source;

        public AsAsyncEnumerable(IEnumerable<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsAsyncEnumerator<T>(_source.GetEnumerator());
    }
}