using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Features;

namespace NCoreUtils.Storage
{
    public interface IStorageProvider : IFeatureProvider
    {
        IAsyncEnumerable<IStorageRoot> GetRootsAsync();

        Task<IStoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken));
    }
}