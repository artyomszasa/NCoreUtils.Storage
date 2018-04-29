using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using NCoreUtils.Linq;
using StorageClient = Google.Cloud.Storage.V1.StorageClient;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageProvider : IStorageProvider
    {
        protected const string GoogleStorageScheme = "gs";

        public IFeatureCollection Features { get; }

        public IContentAnalyzer ContentAnalyzer { get; }

        public GoogleCloudStorageOptions Options { get; }

        public ILogger Logger { get; }

        public StorageProvider(
            IFeatureCollection<IStorageProvider> features,
            ILogger<StorageProvider> logger,
            IContentAnalyzer contentAnalyzer = null,
            GoogleCloudStorageOptions options = null)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }
            var googleFeatures = new FeatureCollectionBuilder();
            googleFeatures.AddFeature<ICacheControlFeature>(new CacheControlFeature());
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

        public virtual Task<StorageClient> CreateStorageClientAsync() => StorageClient.CreateAsync();

        public virtual IAsyncEnumerable<StorageRoot> GetRootsAsync()
        {
            return DelayedAsyncEnumerable.Delay(async cancellationToken =>
            {
                var client = await StorageClient.CreateAsync().ConfigureAwait(false);
                return client.ListBucketsAsync(Options.ProjectId)
                    .Select(bucket => new StorageRoot(this, bucket.Name));
            });
        }

        public virtual async Task<StoragePath> ResolveAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GoogleStorageScheme == uri.Scheme)
            {
                var client = await CreateStorageClientAsync().ConfigureAwait(false);
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
            }
            return null;
        }
    }
}