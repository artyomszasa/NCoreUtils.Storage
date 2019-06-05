using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using NCoreUtils.Progress;
using GoogleObject = Google.Apis.Storage.v1.Data.Object;
using GoogleObjectAccessControl = Google.Apis.Storage.v1.Data.ObjectAccessControl;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageRoot : StoragePath, IStorageRoot
    {
        const long MaxBufferSize = 50 * 1024 * 1024; // 50 MB
        static readonly ListObjectsOptions _delimiterOptions = new ListObjectsOptions
        {
            Delimiter = "/"
        };

        IStorageProvider IStorageRoot.StorageProvider => StorageProvider;

        public override string Name => BucketName;

        public StorageProvider StorageProvider { get; }

        public string BucketName { get; }

        public override Uri Uri => new Uri($"gs://{BucketName}");

        public StorageRoot(StorageProvider storageProvider, string bucketName)
            : base()
        {
            if (null == bucketName)
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException("Bucket name must be a non-empty string.", nameof(bucketName));
            }
            StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            BucketName = bucketName;
            StorageRoot = this;
        }

        async Task<IStorageFolder> IStorageContainer.CreateFolderAsync(string name, IProgress progress, CancellationToken cancellationToken)
            => await CreateFolderAsync(name, progress, cancellationToken);

        async Task<IStorageRecord> IStorageContainer.CreateRecordAsync(string name, Stream contents, string contentType, IProgress progress, CancellationToken cancellationToken)
            => await CreateRecordAsync(name, contents, contentType, progress, cancellationToken);


        internal async IAsyncEnumerable<StorageItem> EnumerateContentsAsync(string localName, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var entry = await UseStorageClient();
            var name = localName?.Trim('/');
            var responses = entry.Client.ListObjectsAsync(BucketName, string.IsNullOrEmpty(name) ? name : name + '/', _delimiterOptions)
                .AsRawResponses();
            using (var responseEnumerator = responses.GetEnumerator())
            {
                while (await responseEnumerator.MoveNext(cancellationToken))
                {
                    var response = responseEnumerator.Current;
                    if (null != response.Prefixes)
                    {
                        foreach (var prefix in response.Prefixes)
                        {
                            yield return new StorageFolder(this, prefix);
                        }
                    }
                    if (null != response.Items)
                    {
                        foreach (var googleObject in response.Items)
                        {
                            yield return new StorageRecord(this, googleObject.Name, googleObject);
                        }
                    }
                }
            }
        }

        // protected internal virtual Task<IAsyncEnumerable<StorageItem>> GetContentsAsync(string localName, CancellationToken cancellationToken)
        // {
        //     return Task.FromResult(EnumerateContentsAsync(localName, cancellationToken));
        // }

        async IAsyncEnumerable<StorageRecord> RecursiveCollectObjectsAsync(string localPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in EnumerateContentsAsync(localPath, cancellationToken).WithCancellation(cancellationToken))
            {
                switch (item)
                {
                    case StorageRecord record:
                        yield return record;
                        break;
                    case StorageFolder folder:
                        await foreach (var subitem in RecursiveCollectObjectsAsync(folder.LocalPath).WithCancellation(cancellationToken))
                        {
                            yield return subitem;
                        }
                        break;
                }
            }
        }

        protected virtual IAsyncEnumerable<StorageRecord> RecursiveCollectObjectsAsync(string localPath)
            => Internal.AsyncEnumerable.FromCancellable(cancellationToken => RecursiveCollectObjectsAsync(localPath, cancellationToken));

        internal StorageSecurity GetPermissions(GoogleObject obj)
        {
            var builder = ImmutableDictionary.CreateBuilder<StorageActor, StoragePermissions>();
            if (null != obj.Acl)
            {
                foreach (var objectAccessControl in obj.Acl)
                {
                    switch (objectAccessControl.Entity)
                    {
                        case "allAuthenticatedUsers":
                            builder.Add(StorageActor.Authenticated, objectAccessControl.GetStoragePermissions());
                            break;
                        case "allUsers":
                            builder.Add(StorageActor.Public, objectAccessControl.GetStoragePermissions());
                            break;
                        case string entity when entity.StartsWith("user-"):
                            builder.Add(StorageActor.User(objectAccessControl.Email ?? objectAccessControl.EntityId), objectAccessControl.GetStoragePermissions());
                            break;
                        case string entity when entity.StartsWith("group-"):
                            builder.Add(StorageActor.Group(objectAccessControl.Email ?? objectAccessControl.EntityId), objectAccessControl.GetStoragePermissions());
                            break;
                    }
                }
            }
            return new StorageSecurity(builder.ToImmutable());
        }

        internal IEnumerable<GoogleObjectAccessControl> CreateObjectAccessControlList(IStorageSecurity storageSecurity)
        {
            foreach (var kv in storageSecurity)
            {
                var actor = kv.Key;
                var ps = kv.Value;
                if (ps.TryGetObjectAccessControl(actor, out var ac))
                {
                    yield return ac;
                }
            }
        }

        public ValueTask<StorageProvider.StorageClientEntry> UseStorageClient() => StorageProvider.UseStorageClient();

        public Task UseStorageClient(Func<StorageClient, Task> action) => StorageProvider.UseStorageClient(action);

        public Task<T> UseStorageClient<T>(Func<StorageClient, Task<T>> action) => StorageProvider.UseStorageClient(action);

        public virtual Task<StorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.SetTotal(1);
            progress.SetValue(1);
            return Task.FromResult(new StorageFolder(this, name));
        }

        public virtual async Task<StorageRecord> CreateRecordAsync(string name, Stream contents, string contentType, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
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
            long? contentLength;
            try
            {
                contentLength = contents.Length;
            }
            catch (NotSupportedException)
            {
                contentLength = null;
            }
            GoogleObject googleObject;
            if (contentLength.HasValue && contents.CanSeek)
            {
                googleObject = await UploadSeekableStream(contents, contentLength.Value);
            }
            else
            {
                if (string.IsNullOrEmpty(contentType))
                {
                    // if no content length can be deternied and content type is not set --> stream must be buffered....
                    using (var buffer = new MemoryStream())
                    {
                        await contents.CopyToAsync(buffer);
                        buffer.Seek(0, SeekOrigin.Begin);
                        googleObject = await UploadSeekableStream(buffer, buffer.Length);
                    }
                }
                else
                {
                    uploadProgress.SetTotal(1);
                    uploadProgress.SetValue(0);
                    var options = new UploadObjectOptions
                    {
                        ChunkSize = StorageProvider.Options.ChunkSize,
                        PredefinedAcl = StorageProvider.Options.PredefinedAcl
                    };
                    googleObject = new GoogleObject
                    {
                        Bucket = BucketName,
                        Name = name.TrimStart('/'),
                        CacheControl = StorageProvider.Options.DefaultCacheControl,
                        ContentDisposition = StorageProvider.Options.DefaultContentDisposition,
                        ContentEncoding = StorageProvider.Options.DefaultContentEncoding,
                        ContentLanguage = StorageProvider.Options.DefaultContentLanguage,
                        ContentType = contentType
                    };
                    googleObject = await UseStorageClient(client => client.UploadObjectAsync(googleObject, contents, options, cancellationToken)).ConfigureAwait(false);
                    uploadProgress.SetValue(1);
                }
            }
            var result = new StorageRecord(this, name, googleObject);
            miscProgress.SetValue(2);
            return result;

            async Task<GoogleObject> UploadSeekableStream(Stream stream, long clength)
            {
                uploadProgress.SetTotal(clength);
                uploadProgress.SetValue(0);
                string mediaType;
                if (string.IsNullOrEmpty(contentType))
                {
                    mediaType = await StorageProvider.GetMediaTypeAsync(stream, name, cancellationToken).ConfigureAwait(false);
                    stream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    mediaType = contentType;
                }
                miscProgress.SetValue(1);
                var options = new UploadObjectOptions
                {
                    ChunkSize = StorageProvider.Options.ChunkSize,
                    PredefinedAcl = StorageProvider.Options.PredefinedAcl
                };
                googleObject = new GoogleObject
                {
                    Bucket = BucketName,
                    Name = name.TrimStart('/'),
                    CacheControl = StorageProvider.Options.DefaultCacheControl,
                    ContentDisposition = StorageProvider.Options.DefaultContentDisposition,
                    ContentEncoding = StorageProvider.Options.DefaultContentEncoding,
                    ContentLanguage = StorageProvider.Options.DefaultContentLanguage,
                    ContentType = mediaType ?? "application/octet-stream",
                    Size = (ulong)clength
                };
                return await UseStorageClient(client => client.UploadObjectAsync(googleObject, stream, options, cancellationToken, uploadProgress)).ConfigureAwait(false);
            }
        }

        public virtual async Task DeleteRecursiveAsync(StorageItem item, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (item)
            {
                case StorageRecord record:
                    progress.SetTotal(1);
                    await UseStorageClient(client => client.DeleteObjectAsync(record.GoogleObject)).ConfigureAwait(false);
                    progress.SetValue(1);
                    break;
                case StorageFolder folder:
                    var records = await Internal.AsyncEnumerable.ToList(RecursiveCollectObjectsAsync(folder.LocalPath), cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.SetTotal(records.Count);
                    await UseStorageClient(async client =>
                    {
                        foreach (var record in records)
                        {
                            await client.DeleteObjectAsync(record.GoogleObject).ConfigureAwait(false);
                            if (null != progress)
                            {
                                ++progress.Value;
                            }
                        }
                    });
                    break;
                default:
                    progress.SetTotal(1);
                    progress.SetValue(1);
                    break;
            }
        }

        public Task DownloadRecordAsync(StorageRecord record, Stream destination, int? chunkSize = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UseStorageClient(client =>
            {
                var options = new DownloadObjectOptions();
                if (chunkSize.HasValue)
                {
                    options.ChunkSize = chunkSize.Value;
                }
                return client.DownloadObjectAsync(record.GoogleObject, destination, options, cancellationToken);
            });
        }

        public virtual async Task<Stream> CreateReadableStreamAsync(StorageRecord record, CancellationToken cancellationToken)
        {
            // TODO: avoid using large in-memory buffers...
            Stream buffer;
            if (record.Size > MaxBufferSize)
            {
                buffer = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.DeleteOnClose);
            }
            else
            {
                buffer = new MemoryStream((int)record.Size);
            }
            await DownloadRecordAsync(record, buffer, cancellationToken: cancellationToken);
            buffer.Seek(0, SeekOrigin.Begin);
            return buffer;
        }

        public IAsyncEnumerable<IStorageItem> GetContentsAsync()
            => Internal.AsyncEnumerable.FromCancellable(cancellationToken => EnumerateContentsAsync(null, cancellationToken));

        public virtual Task<StorageRecord> RenameAsync(StorageRecord record, string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            progress.SetTotal(3);
            progress.SetValue(0);
            return UseStorageClient(async client =>
            {
                var slashIndex = record.LocalPath.LastIndexOf('/');
                var folder = -1 == slashIndex ? "" : record.LocalPath.Substring(0, slashIndex + 1);
                var newPath = folder + name;
                progress.SetValue(1);
                var gobj = await client.CopyObjectAsync(BucketName, record.LocalPath, BucketName, newPath, cancellationToken: cancellationToken).ConfigureAwait(false);
                progress.SetValue(2);
                await client.DeleteObjectAsync(record.GoogleObject).ConfigureAwait(false);
                progress.SetValue(3);
                return new StorageRecord(this, newPath, gobj);
            });
        }

        public virtual async Task UpdateContentAsync(StorageRecord record, Stream contents, string contentType, IProgress progress, CancellationToken cancellationToken)
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
            long? contentLength;
            try
            {
                contentLength = contents.Length;
            }
            catch (NotSupportedException)
            {
                contentLength = null;
            }
            GoogleObject googleObject;
            if (contentLength.HasValue && contents.CanSeek)
            {
                googleObject = await UpdateSeekableStream(contents, contentLength.Value).ConfigureAwait(false);
            }
            else
            {
                if (string.IsNullOrEmpty(contentType))
                {
                    // if no content length can be deternied and content type is not set --> stream must be buffered....
                    using (var buffer = new MemoryStream())
                    {
                        await contents.CopyToAsync(buffer).ConfigureAwait(false);
                        buffer.Seek(0, SeekOrigin.Begin);
                        googleObject = await UpdateSeekableStream(buffer, buffer.Length);
                    }
                }
                else
                {
                    uploadProgress.SetTotal(1);
                    uploadProgress.SetValue(0);
                    string mediaType = contentType;
                    miscProgress.SetValue(1);
                    var options = new UploadObjectOptions
                    {
                        ChunkSize = StorageProvider.Options.ChunkSize
                    };
                    record.GoogleObject.Crc32c = null;
                    record.GoogleObject.ComponentCount = null;
                    record.GoogleObject.ETag = null;
                    record.GoogleObject.Md5Hash = null;
                    record.GoogleObject.ContentType = mediaType ?? "application/octet-stream";
                    googleObject = await UseStorageClient(async client =>
                    {
                        record.GoogleObject = await client.UploadObjectAsync(record.GoogleObject, contents, options, cancellationToken).ConfigureAwait(false);
                        miscProgress.SetValue(2);
                        uploadProgress.SetValue(1);
                        return record.GoogleObject;
                    }).ConfigureAwait(false);
                }
            }
            record.GoogleObject = googleObject;

            async Task<GoogleObject> UpdateSeekableStream(Stream stream, long clength)
            {
                uploadProgress.SetTotal(clength);
                uploadProgress.SetValue(0);
                string mediaType = contentType ?? await StorageProvider.GetMediaTypeAsync(stream, record.Name, cancellationToken).ConfigureAwait(false);
                miscProgress.SetValue(1);
                var options = new UploadObjectOptions
                {
                    ChunkSize = StorageProvider.Options.ChunkSize
                };
                record.GoogleObject.Size = (ulong)clength;
                record.GoogleObject.Crc32c = null;
                record.GoogleObject.ComponentCount = null;
                record.GoogleObject.ETag = null;
                record.GoogleObject.Md5Hash = null;
                record.GoogleObject.ContentType = mediaType ?? "application/octet-stream";
                return await UseStorageClient(async client =>
                {
                    record.GoogleObject = await client.UploadObjectAsync(record.GoogleObject, stream, options, cancellationToken, uploadProgress).ConfigureAwait(false);
                    miscProgress.SetValue(2);
                    return record.GoogleObject;
                }).ConfigureAwait(false);
            }
        }

        public virtual async Task<GoogleObject> UpdateObjectAclAsync(GoogleObject gObject, IList<Google.Apis.Storage.v1.Data.ObjectAccessControl> acl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool success = false;
            var orignalAcl = gObject.Acl;
            try
            {
                gObject.Acl = acl;
                return await UseStorageClient(async client =>
                {
                    var res = await client.UpdateObjectAsync(gObject, cancellationToken: cancellationToken).ConfigureAwait(false);
                    success = true;
                    return res;
                }).ConfigureAwait(false);
            }
            finally
            {
                if (!success)
                {
                    gObject.Acl = orignalAcl;
                }
            }
        }

        public override Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken))
            => Task.FromResult<IStoragePath>(null);
    }
}