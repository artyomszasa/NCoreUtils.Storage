using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;

namespace NCoreUtils.Storage.FileSystem
{
    public abstract class StorageProvider : IStorageProvider
    {
        public IFeatureCollection Features { get; }

        public IContentAnalyzer ContentAnalyzer { get; }

        public ILogger Logger { get; }

        protected StorageProvider(IFeatureCollection<IStorageProvider> features, ILogger<StorageProvider> logger, IContentAnalyzer contentAnalyzer)
        {
            Features = features ?? throw new ArgumentNullException(nameof(features));
            ContentAnalyzer = contentAnalyzer;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract IEnumerable<StorageRoot> GetFileSystemRoots();

        protected internal abstract Task<StoragePath> ResolvePathAsync(string absolutePath, CancellationToken cancellationToken);

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync() => GetFileSystemRoots().ToAsyncEnumerable();

        public async Task<IStoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
            => uri.Scheme == "file" ? await ResolvePathAsync(uri.AbsolutePath, cancellationToken) : null;
    }
}