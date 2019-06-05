using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Features;

namespace NCoreUtils.Storage
{
    public class CompositeStorageProvider : IStorageProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollectionBuilder().Build();

        public ImmutableArray<IStorageProvider> StorageProviders { get; }

        public CompositeStorageProvider(ImmutableArray<IStorageProvider> storageProviders)
            => StorageProviders = storageProviders;

        async IAsyncEnumerable<IStorageRoot> GetRootsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var provider in StorageProviders)
            {
                await foreach (var root in provider.GetRootsAsync().WithCancellation(cancellationToken))
                {
                    yield return root;
                }
            }
        }

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync()
            => Internal.AsyncEnumerable.FromCancellable(GetRootsAsync);

        public async Task<IStoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var provider in StorageProviders)
            {
                var path = await provider.ResolveAsync(uri, cancellationToken);
                if (null != path)
                {
                    return path;
                }
            }
            return null;
        }
    }
}