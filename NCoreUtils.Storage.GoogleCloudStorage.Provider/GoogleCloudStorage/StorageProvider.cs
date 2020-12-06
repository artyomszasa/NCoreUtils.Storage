using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using NCoreUtils.Storage.Features;
using StorageClient = Google.Cloud.Storage.V1.StorageClient;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageProvider : IStorageProvider
    {
        [ExcludeFromCodeCoverage]
        internal sealed class ClientPool
        {
            readonly ConcurrentQueue<StorageClient> _queue = new ConcurrentQueue<StorageClient>();

            [ExcludeFromCodeCoverage]
            public ValueTask<StorageClient> GetAsync()
            {
                if (_queue.TryDequeue(out var client))
                {
                    return new ValueTask<StorageClient>(client);
                }
                return new ValueTask<StorageClient>(StorageClient.CreateAsync());
            }

            [ExcludeFromCodeCoverage]
            public void Return(StorageClient client) => _queue.Enqueue(client);
        }

        [ExcludeFromCodeCoverage]
        public sealed class StorageClientEntry : IAsyncDisposable
        {
            readonly ClientPool _pool;

            StorageClient _client;

            public StorageClient Client =>  _client;

            internal StorageClientEntry(ClientPool pool, StorageClient client)
            {
                _pool = pool;
                _client = client;
            }

            public ValueTask DisposeAsync()
            {
                var client = Interlocked.Exchange(ref _client, null);
                if (null != client)
                {
                    _pool.Return(client);
                }
                return default;
            }
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
            googleFeatures.AddFeature<ILoggerFeature>(new LoggerFeature());
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
        internal virtual ValueTask<StorageClient> GetPooledStorageClientAsync() => _pool.GetAsync();

        [ExcludeFromCodeCoverage]
        internal virtual void ReturnPooledStorageClientAsync(StorageClient client) => _pool.Return(client);

        async Task<StorageClientEntry> CreateEntryAsync(Task<StorageClient> client)
        {
            return new StorageClientEntry(_pool, await client);
        }

        public ValueTask<StorageClientEntry> UseStorageClient()
        {
            var client = GetPooledStorageClientAsync();
            if (client.IsCompleted)
            {
                return new ValueTask<StorageClientEntry>(new StorageClientEntry(_pool, client.Result));
            }
            return new ValueTask<StorageClientEntry>(CreateEntryAsync(client.AsTask()));
        }

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

        async IAsyncEnumerable<StorageRoot> GetRootsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using (var entry = await UseStorageClient())
            {
                var buckerEnumerable = entry.Client.ListBucketsAsync(Options.ProjectId);
                await foreach (var bucket in buckerEnumerable)
                {
                    yield return new StorageRoot(this, bucket.Name);
                }
            }
        }

        public virtual IAsyncEnumerable<StorageRoot> GetRootsAsync()
            => Internal.AsyncEnumerable.FromCancellable(GetRootsAsync);

        public virtual Task<StoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GoogleStorageScheme == uri.Scheme)
            {
                return UseStorageClient<StoragePath>(async client =>
                {
                    try
                    {
                        var bucket = await client.GetBucketAsync(uri.Host, cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (uri.LocalPath == "/" || string.IsNullOrEmpty(uri.LocalPath))
                        {
                            return new StorageRoot(this, bucket.Name);
                        }
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