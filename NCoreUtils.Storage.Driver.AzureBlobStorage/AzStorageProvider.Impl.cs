using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace NCoreUtils.Storage.AzureBlobStorage
{
    public partial class AzStorageProvider
    {
        private static StorageStats ToStats(bool exists, BlobProperties props)
            => new StorageStats(
                exists: exists,
                size: props.ContentLength,
                mediaType: props.ContentType,
                created: props.CreatedOn,
                updated: props.LastModified,
                acl: default
            );

        private static StorageStats ToStats(bool exists, BlobItemProperties props)
            => new StorageStats(
                exists: exists,
                size: props.ContentLength,
                mediaType: props.ContentType,
                created: props.CreatedOn,
                updated: props.LastModified,
                acl: default
            );

        private static StorageStats ToStats(BlobItem item)
            => ToStats(!item.Deleted, item.Properties);

        private static async Task<TResult> UnsafeTaskCast<TSource, TResult>(Task<TSource> task)
        {
            var res = await task.ConfigureAwait(false);
            return (TResult)(object)res!;
        }

        private const int DefaultBufferSize = 16 * 1024;

        private const int MaxCharStackAllocSize = 8 * 1024;

        public AzStorageDriver Driver { get; }

        public BlobContainerClient ContainerClient { get; }

        public string ContainerName { get; }

        public string? PathPrefix { get; }

        public AzStorageProvider(AzStorageDriver driver, string containerName, string? pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException($"'{nameof(containerName)}' cannot be null or whitespace.", nameof(containerName));
            }
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            ContainerClient = driver.Client.GetBlobContainerClient(containerName);
            ContainerName = containerName;
            PathPrefix = pathPrefix;
        }

        private string GetFullName(in GenericSubpath subpath)
        {
            if (string.IsNullOrEmpty(PathPrefix))
            {
                return subpath.ToString();
            }
            {
                // TODO: usage stats needed: exact buffer sizes may be better
                Span<char> buffer = stackalloc char[MaxCharStackAllocSize];
                var builder = new SpanBuilder(buffer);
                if (builder.TryAppend(PathPrefix) && builder.TryAppend(subpath))
                {
                    return builder.ToString();
                }
            }
            return PathPrefix + subpath.ToString();
        }

        private GenericSubpath GetSubpath(string source)
        {
            if (string.IsNullOrEmpty(PathPrefix))
            {
                return GenericSubpath.Parse(source);
            }
            if (source.StartsWith(PathPrefix))
            {
                return GenericSubpath.Parse(source, PathPrefix.Length);
            }
            throw new InvalidOperationException($"Unable to extract subpath from \"{source}\" with path prefix = \"{PathPrefix}\".");
        }

        protected virtual async Task<Stream> DoCreateReadableStream(string name, CancellationToken cancellationToken)
        {
            var blobClient = ContainerClient.GetBlobClient(name);
            return await blobClient.OpenReadAsync(new BlobOpenReadOptions(false), cancellationToken);
        }

        protected virtual async ValueTask<IStorageRecord> DoCreateRecordAsync(
            GenericSubpath subpath,
            Stream contents,
            string? contentType = default,
            string? cacheControl = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default)
        {
            var blobClient = ContainerClient.GetBlobClient(GetFullName(subpath));
            var options = new BlobUploadOptions();
            options.HttpHeaders.CacheControl = cacheControl;
            options.HttpHeaders.ContentType = contentType;
            await blobClient.UploadAsync(contents, options, cancellationToken);
            return new AzStorageRecord(this, subpath);
        }

        protected virtual async Task DoDeleteAsync(string name, CancellationToken cancellationToken)
        {
            var blobClient = ContainerClient.GetBlobClient(name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        protected virtual async IAsyncEnumerable<IStorageItem> DoEnumerateItemsAsync(
            string subpath,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var dedup = new HashSet<string>();
            await foreach(var item in ContainerClient.GetBlobsByHierarchyAsync(delimiter: "/", prefix: subpath, cancellationToken: cancellationToken))
            {
                if (item.IsPrefix)
                {
                    yield return new AzStorageFolder(this, GetSubpath(item.Prefix));
                }
                else
                {
                    yield return new AzStorageRecord(this, GetSubpath(item.Blob.Name), ToStats(item.Blob));
                }
            }
        }

        protected async Task<StorageStats> DoGetStatsAsync(string fullName, CancellationToken cancellationToken = default)
        {
            var blobClient = ContainerClient.GetBlobClient(fullName);
            var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var props = response.Value;
            return props is null ? StorageStats.DoesNotExist : ToStats(true, props);
        }

        protected async Task<IStorageRecord> DoRenameRecordAsync(string sourceName, GenericSubpath targetName, CancellationToken cancellationToken = default)
        {
            var sourceClient = ContainerClient.GetBlobClient(sourceName);
            var targetClient = ContainerClient.GetBlobClient(GetFullName(targetName));
            await targetClient.SyncCopyFromUriAsync(sourceClient.Uri, cancellationToken: cancellationToken);
            await sourceClient.DeleteIfExistsAsync();
            return new AzStorageRecord(this, targetName);
        }

        protected async Task<IStoragePath> DoResolveAsync(GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            var blobClient = ContainerClient.GetBlobClient(GetFullName(subpath));
            var result = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return result.Value is null
                ? (IStoragePath)new AzStorageFolder(this, in subpath)
                : new AzStorageRecord(this, in subpath, ToStats(true, result.Value));
        }

        protected virtual async Task DoUpdateRecordAsync(GenericSubpath subpath, Stream contents, string? contentType = null, CancellationToken cancellationToken = default)
        {
            var blobClient = ContainerClient.GetBlobClient(GetFullName(subpath));
            var result = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            await DoCreateRecordAsync(
                subpath,
                contents,
                contentType,
                result.Value.CacheControl,
                true,
                default,
                cancellationToken
            );
        }
    }
}