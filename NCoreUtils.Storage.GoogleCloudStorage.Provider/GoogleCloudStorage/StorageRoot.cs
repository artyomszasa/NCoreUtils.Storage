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
        const long MaxBufferSize = 50 * 1024 * 1024; // 50 MB
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

        protected virtual IAsyncEnumerable<StorageRecord> RecursiveCollectObjectsAsync(string localPath)
        {
            return DelayedAsyncEnumerable.Delay(async cancellationToken =>
            {
                var contents = await GetContentsAsync(localPath, cancellationToken).ConfigureAwait(false);
                return contents
                    .SelectMany(item =>
                    {
                        switch (item)
                        {
                            case StorageRecord record:
                                return new [] { record }.ToAsyncEnumerable();
                            case StorageFolder folder:
                                return RecursiveCollectObjectsAsync(folder.LocalPath);
                            default:
                                return new StorageRecord[0].ToAsyncEnumerable();
                        }
                    });
            });
        }

        public Task<StorageClient> CreateStorageClientAsync() => StorageProvider.CreateStorageClientAsync();

        public virtual Task<StorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.SetTotal(1);
            progress.SetValue(1);
            return Task.FromResult(new StorageFolder(this, name));
        }

        public virtual async Task<StorageRecord> CreateRecordAsync(string name, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
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

        public virtual async Task DeleteRecursiveAsync(StorageItem item, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (item)
            {
                case StorageRecord record:
                    progress.SetTotal(1);
                    {
                        var client = await CreateStorageClientAsync().ConfigureAwait(false);
                        await client.DeleteObjectAsync(record.GoogleObject).ConfigureAwait(false);
                    }
                    progress.SetValue(1);
                    break;
                case StorageFolder folder:
                    var records = await RecursiveCollectObjectsAsync(folder.LocalPath).ToList(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.SetTotal(records.Count);
                    {
                        var client = await CreateStorageClientAsync().ConfigureAwait(false);
                        foreach (var record in records)
                        {
                            await client.DeleteObjectAsync(record.GoogleObject).ConfigureAwait(false);
                            if (null != progress)
                            {
                                ++progress.Value;
                            }
                        }
                    }
                    break;
                default:
                    progress.SetTotal(1);
                    progress.SetValue(1);
                    break;
            }
        }

        public virtual async Task<Stream> CreateReadableStreamAsync(StorageRecord record, CancellationToken cancellationToken)
        {
            // TODO: avoid using large in-memory buffers...
            Stream buffer;
            if (record.Size > MaxBufferSize)
            {
                buffer = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8091, FileOptions.DeleteOnClose);
            }
            else
            {
                buffer = new MemoryStream((int)record.Size);
            }
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            await client.DownloadObjectAsync(record.GoogleObject, buffer, cancellationToken: cancellationToken).ConfigureAwait(false);
            buffer.Seek(0, SeekOrigin.Begin);
            return buffer;
        }

        public IAsyncEnumerable<IStorageItem> GetContentsAsync()
            => DelayedAsyncEnumerable.Delay(cancellationToken => GetContentsAsync(null, cancellationToken));

        public virtual async Task<StorageRecord> RenameAsync(StorageRecord record, string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            progress.SetTotal(3);
            progress.SetValue(0);
            var client = await CreateStorageClientAsync().ConfigureAwait(false);
            var slashIndex = record.LocalPath.LastIndexOf('/');
            var folder = -1 == slashIndex ? "" : record.LocalPath.Substring(0, slashIndex + 1);
            var newPath = folder + name;
            progress.SetValue(1);
            var gobj = await client.CopyObjectAsync(BucketName, record.LocalPath, BucketName, newPath, cancellationToken: cancellationToken).ConfigureAwait(false);
            progress.SetValue(2);
            await client.DeleteObjectAsync(record.GoogleObject).ConfigureAwait(false);
            progress.SetValue(3);
            return new StorageRecord(this, newPath, gobj);
        }

        public virtual async Task UpdateContentAsync(StorageRecord record, Stream contents, IProgress progress, CancellationToken cancellationToken)
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
            string mediaType = await StorageProvider.GetMediaTypeAsync(contents, record.Name, cancellationToken).ConfigureAwait(false);
            miscProgress.SetValue(1);
            var options = new UploadObjectOptions
            {
                ChunkSize = StorageProvider.Options.ChunkSize,
                PredefinedAcl = StorageProvider.Options.PredefinedAcl
            };
            record.GoogleObject.Size = (ulong)contents.Length;
            record.GoogleObject.Crc32c = null;
            record.GoogleObject.ComponentCount = null;
            record.GoogleObject.ETag = null;
            record.GoogleObject.Md5Hash = null;
            record.GoogleObject.ContentType = mediaType ?? "application/octet-stream";
            var client = await CreateStorageClientAsync().ConfigureAwait(false);
            record.GoogleObject = await client.UploadObjectAsync(record.GoogleObject, contents, options, cancellationToken, uploadProgress).ConfigureAwait(false);
            miscProgress.SetValue(2);
        }
    }
}