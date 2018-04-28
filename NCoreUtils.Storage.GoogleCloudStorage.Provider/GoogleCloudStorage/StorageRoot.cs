using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using NCoreUtils.Linq;
using NCoreUtils.Progress;
using GoogleObject = Google.Apis.Storage.v1.Data.Object;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageRoot : IStorageRoot
    {
        static readonly ListObjectsOptions _delimiterOptions = new ListObjectsOptions
        {
            Delimiter = "/"
        };

        IStorageRoot IStoragePath.StorageRoot => this;

        IStorageProvider IStorageRoot.StorageProvider => StorageProvider;

        public StorageProvider StorageProvider { get; }

        public string BucketName { get; }

        public Uri Uri => new Uri($"gs://{BucketName}");

        public StorageRoot(StorageProvider storageProvider, string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException("Bucket name must be a non-empty string.", nameof(bucketName));
            }
            StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            BucketName = bucketName;
        }

        async Task<IStorageFolder> IStorageContainer.CreateFolderAsync(string name, IProgress progress, CancellationToken cancellationToken)
            => await CreateFolderAsync(name, progress, cancellationToken);

        async Task<IStorageRecord> IStorageContainer.CreateRecordAsync(string name, Stream contents, IProgress progress, CancellationToken cancellationToken)
            => await CreateRecordAsync(name, contents, progress, cancellationToken);


        protected internal virtual async Task<IAsyncEnumerable<StorageItem>> GetContentsAsync(string localName, CancellationToken cancellationToken)
        {
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            var name = localName?.Trim('/');
            return client.ListObjectsAsync(BucketName, string.IsNullOrEmpty(name) ? name : name + '/', _delimiterOptions)
                .AsRawResponses()
                .SelectMany(response =>
                {
                    var folders = null == response.Prefixes
                        ? Enumerable.Empty<StorageItem>()
                        : response.Prefixes
                            .Select(prefix => (StorageItem)new StorageFolder(this, prefix));
                    var records = null == response.Items
                        ? Enumerable.Empty<StorageItem>()
                        : response.Items
                            .Select(googleObject => (StorageItem)new StorageRecord(this, googleObject.Name, googleObject));
                    return folders.Concat(records).ToAsyncEnumerable();
                });
        }


        public Task<StorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.SetTotal(1);
            progress.SetValue(1);
            return Task.FromResult(new StorageFolder(this, name));
        }

        public async Task<StorageRecord> CreateRecordAsync(string name, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GoogleProgressSource uploadProgress = null;
            ProgressReporter miscProgress = null;
            if (null != progress)
            {
                uploadProgress = new GoogleProgressSource();
                miscProgress = new NCoreUtils.Progress.ProgressReporter();
                new SummaryProgress(progress, uploadProgress, miscProgress);
            }
            miscProgress.SetTotal(2);
            miscProgress.SetValue(0);
            uploadProgress.SetTotal(contents.Length);
            uploadProgress.SetValue(0);
            string mediaType = await StorageProvider.GetMediaTypeAsync(contents, name, cancellationToken).ConfigureAwait(false);
            miscProgress.SetValue(1);
            var options = new UploadObjectOptions
            {
                ChunkSize = StorageProvider.Options.ChunkSize,
                PredefinedAcl = StorageProvider.Options.PredefinedAcl
            };
            var googleObject = new GoogleObject
            {
                Bucket = BucketName,
                Name = name.TrimStart('/'),
                CacheControl = StorageProvider.Options.DefaultCacheControl,
                ContentDisposition = StorageProvider.Options.DefaultContentDisposition,
                ContentEncoding = StorageProvider.Options.DefaultContentEncoding,
                ContentLanguage = StorageProvider.Options.DefaultContentLanguage,
                ContentType = mediaType ?? "application/octet-stream",
                Size = (ulong)contents.Length
            };
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            googleObject = await client.UploadObjectAsync(googleObject, contents, options, cancellationToken, uploadProgress).ConfigureAwait(false);
            var result = new StorageRecord(this, name, googleObject);
            miscProgress.SetValue(2);
            return result;
        }

        public IAsyncEnumerable<IStorageItem> GetContentsAsync()
            => DelayedAsyncEnumerable.Delay(cancellationToken => GetContentsAsync(null, cancellationToken));
    }
}