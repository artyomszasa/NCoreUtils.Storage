using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using NCoreUtils.Linq;
using NCoreUtils.Storage.Features;
using StorageClient = Google.Cloud.Storage.V1.StorageClient;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageProvider : IStorageProvider
    {
        [ExcludeFromCodeCoverage]
        sealed class ClientPool
        {
            readonly ConcurrentQueue<StorageClient> _queue = new ConcurrentQueue<StorageClient>();

            [ExcludeFromCodeCoverage]
            public Task<StorageClient> GetAsync()
            {
                if (_queue.TryDequeue(out var client))
                {
                    return Task.FromResult(client);
                }
                return StorageClient.CreateAsync();
            }

            [ExcludeFromCodeCoverage]
            public void Return(StorageClient client) => _queue.Enqueue(client);
        }

        private ClientPool _pool = new ClientPool();

        protected const string GoogleStorageScheme = "gs";

        public IFeatureCollection Features { get; }

        public IContentAnalyzer ContentAnalyzer { get; }

        public GoogleCloudStorageOptions Options { get; }

        public ILogger Logger { get; }

        public StorageProvider(
            ILogger<StorageProvider> logger,
            IFeatureCollection<IStorageProvider> features = null,
            IContentAnalyzer contentAnalyzer = null,
            GoogleCloudStorageOptions options = null)
        {
            if (features == null)
            {
                features = new FeatureCollectionBuilder().Build<IStorageProvider>();
            }
            var googleFeatures = new FeatureCollectionBuilder();
            googleFeatures.AddFeature<ICacheControlFeature>(new CacheControlFeature());
            googleFeatures.AddFeature<ICreateByPathFeature>(new CreateByPathFeature());
            googleFeatures.AddFeature<IRecordCopyFeature>(new RecordCopyFeature());
            Features = new CompositeFeatureCollection(features, googleFeatures.Build());
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ContentAnalyzer = contentAnalyzer;
            Options = options ?? GoogleCloudStorageOptions.Default;
        }

        IAsyncEnumerable<IStorageRoot> IStorageProvider.GetRootsAsync() => GetRootsAsync();

        async Task<IStoragePath> IStorageProvider.ResolveAsync(Uri uri, CancellationToken cancellationToken)
            => await ResolveAsync(uri, cancellationToken).ConfigureAwait(false);

        protected internal virtual async Task<string> GetMediaTypeAsync(Stream stream, string name, CancellationToken cancellationToken)
        {
            string mediaType;
            if (null != ContentAnalyzer)
            {
                var contentInfo = await ContentAnalyzer.Analyze(stream, name, true, cancellationToken).ConfigureAwait(false);
                if (null != contentInfo && !string.IsNullOrEmpty(contentInfo.MediaType))
                {
                    Logger.LogDebug("Successfully detected media type for \"{0}\" as \"{1}\".", name, contentInfo.MediaType);
                    mediaType = contentInfo.MediaType;
                }
                else
                {
                    Logger.LogDebug("Unable to detect media type for \"{0}\".", name);
                    mediaType = "application/octet-stream";
                }
            }
            else
            {
                Logger.LogDebug("No content type analyzer specified to detect media type for \"{0}\".", name);
                mediaType = "application/octet-stream";
            }
            return mediaType;
        }

        [ExcludeFromCodeCoverage]
        internal virtual Task<StorageClient> GetPooledStorageClientAsync() => _pool.GetAsync();

        [ExcludeFromCodeCoverage]
        internal virtual void ReturnPooledStorageClientAsync(StorageClient client) => _pool.Return(client);

        public async Task UseStorageClient(Func<StorageClient, Task> action)
        {
            var client = await GetPooledStorageClientAsync().ConfigureAwait(false);
            try
            {
                await action(client);
            }
            finally
            {
                ReturnPooledStorageClientAsync(client);
            }
        }

        public async Task<T> UseStorageClient<T>(Func<StorageClient, Task<T>> action)
        {
            var client = await GetPooledStorageClientAsync().ConfigureAwait(false);
            try
            {
                return await action(client);
            }
            finally
            {
                ReturnPooledStorageClientAsync(client);
            }
        }

        public virtual IAsyncEnumerable<StorageRoot> GetRootsAsync()
        {
            return DelayedAsyncEnumerable.Delay(async cancellationToken =>
            {
                var client = await GetPooledStorageClientAsync().ConfigureAwait(false);
                var results = client.ListBucketsAsync(Options.ProjectId)
                    .Select(bucket => new StorageRoot(this, bucket.Name))
                    .Finally(() => ReturnPooledStorageClientAsync(client));
                return results;
            });
        }

        public virtual Task<StoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GoogleStorageScheme == uri.Scheme)
            {
                return UseStorageClient<StoragePath>(async client =>
                {
                    try
                    {
                        var bucket = await client.GetBucketAsync(uri.Host, cancellationToken: cancellationToken).ConfigureAwait(false);
                        try
                        {
                            var googleObject = await client.GetObjectAsync(bucket.Name, uri.LocalPath.TrimStart('/'), cancellationToken: cancellationToken).ConfigureAwait(false);
                            return new StorageRecord(new StorageRoot(this, bucket.Name), uri.LocalPath.TrimStart('/'), googleObject);
                        }
                        catch
                        {
                            return new StorageFolder(new StorageRoot(this, bucket.Name), uri.LocalPath.TrimStart('/'));
                        }
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            return Task.FromResult<StoragePath>(null);
        }
    }
}