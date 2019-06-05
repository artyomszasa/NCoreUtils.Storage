using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.Internal
{
    public static class AsyncEnumerable
    {
        sealed class InjectCancellation<T> : IAsyncEnumerable<T>
        {
            readonly Func<CancellationToken, IAsyncEnumerable<T>> _source;

            public InjectCancellation(Func<CancellationToken, IAsyncEnumerable<T>> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => _source(cancellationToken).GetAsyncEnumerator();
        }

        public static async Task<List<T>> ToList<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            var result = new List<T>();
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                result.Add(item);
            }
            return result;
        }

        public static IAsyncEnumerable<T> FromCancellable<T>(Func<CancellationToken, IAsyncEnumerable<T>> source)
            => new InjectCancellation<T>(source);
    }
}