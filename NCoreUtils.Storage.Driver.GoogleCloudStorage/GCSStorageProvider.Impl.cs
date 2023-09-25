using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public partial class GCSStorageProvider
    {
        private const int DefaultBufferSize = 16 * 1024;

        private const int MaxCharStackAllocSize = 8 * 1024;

        private static bool HasPublicAccess(IStorageSecurity? acl)
        {
            if (acl is null)
            {
                return false;
            }
            return acl.Any((kv) => kv.Key.ActorType == StorageActorType.Public && kv.Value.HasFlag(StoragePermissions.Read));
        }

        private static string? MapPermission(StoragePermissions permissions)
        {
            if (permissions.HasFlag(StoragePermissions.Control) || permissions.HasFlag(StoragePermissions.Write))
            {
                return "OWNER";
            }
            if (permissions.HasFlag(StoragePermissions.Read))
            {
                return "READER";
            }
            return default;
        }

        private static IEnumerable<Google.GoogleAccessControlEntry>? MapAcl(IStorageSecurity? acl)
            => acl is null
                ? default
                : acl.Choose(kv =>
                {
                    var role = MapPermission(kv.Value);
                    if (role is null)
                    {
                        return default;
                    }
                    return kv.Key switch
                    {
                        (StorageActorType.Public, _) => new Google.GoogleAccessControlEntry { Entity = "allUsers", Role = role }.Just(),
                        (StorageActorType.Authenticated, _) => new Google.GoogleAccessControlEntry { Entity = "allAuthenticatedUsers", Role = role }.Just(),
                        (StorageActorType.Group, var id) => new Google.GoogleAccessControlEntry { Entity = $"group-{id}", Role = role }.Just(),
                        (StorageActorType.User, var id) => new Google.GoogleAccessControlEntry { Entity = $"user-{id}", Role = role }.Just(),
                        _ => default
                    };
                });

        private static StorageStats ToStats(Google.GoogleObjectData obj)
            => new StorageStats(
                exists: true,
                size: obj.Size.HasValue ? (long?)unchecked((long)obj.Size.Value) : default,
                mediaType: obj.ContentType,
                created: obj.TimeCreated,
                updated: obj.Updated,
                // FIXME: unmap acl
                acl: default
            );

        IStorageDriver IStorageProvider.Driver => Driver;

        public GoogleCloudStorageUtils Utils { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public string BucketName { get; }

        public string? PathPrefix { get; }

        public GCSStorageDriver Driver { get; }

        public GCSStorageProvider(GCSStorageDriver driver, GoogleCloudStorageUtils utils, IHttpClientFactory httpClientFactory, string bucketName, string? pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException($"'{nameof(bucketName)}' cannot be null or whitespace.", nameof(bucketName));
            }
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            Utils = utils ?? throw new ArgumentNullException(nameof(utils));
            HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            BucketName = bucketName;
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

        protected virtual async Task<Stream> DoCreateReadableStream(string bucket, string name, CancellationToken cancellationToken)
        {
            var buffer = new FileStream(
                Path.GetTempFileName(),
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose
            );
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            await Utils.DownloadAsync(bucket, name, buffer, accessToken, cancellationToken).ConfigureAwait(false);
            buffer.Seek(0L, SeekOrigin.Begin);
            return buffer;
        }

        protected virtual async ValueTask<IStorageRecord> DoCreateRecordAsync(
            GenericSubpath subpath,
            Stream contents,
            string? contentType = default,
            string? cacheControl = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            IProgress<StorageOperationProgress>? progress = default,
            CancellationToken cancellationToken = default)
        {
            long? total = default;
            Action<long>? onProgress = default;
            if (!(progress is null))
            {
                try
                {
                    total = contents.Length;
                }
                catch { }
                onProgress = (sent) => progress.Report(new StorageOperationProgress(sent, total));
            }
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var obj = await Utils.UploadAsync(
                BucketName,
                GetFullName(in subpath),
                contents,
                contentType,
                cacheControl,
                MapAcl(acl),
                accessToken,
                onProgress,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var finalSize = unchecked((long)obj.Size!);
            if (!total.HasValue && !(progress is null))
            {
                progress.Report(new StorageOperationProgress(finalSize, finalSize));
            }
            return new GCSStorageRecord(this, subpath, new StorageStats(
                true,
                finalSize,
                contentType,
                obj.TimeCreated,
                obj.Updated,
                acl
            ));
        }

        protected virtual async Task DoDeleteAsync(GenericSubpath subpath, CancellationToken cancellationToken)
        {
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            await Utils.DeleteAsync(BucketName, GetFullName(in subpath), accessToken, cancellationToken);
        }

        protected virtual async IAsyncEnumerable<IStorageItem> DoEnumerateItemsAsync(GenericSubpath subpath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken);
            await foreach (var page in Utils.ListAsync(BucketName, GetFullName(subpath), includeAcl: true, accessToken: accessToken))
            {
                foreach (var prefix in page.Prefixes)
                {
                    yield return new GCSStorageFolder(this, GetSubpath(prefix));
                }
                foreach (var item in page.Items)
                {
                    yield return new GCSStorageRecord(this, GetSubpath(item.Name!), ToStats(item));
                }
            }
        }

        protected async Task<StorageStats> DoGetStatsAsync(string fullName, CancellationToken cancellationToken = default)
        {
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken);
            var obj = await Utils.GetAsync(BucketName, fullName, accessToken, cancellationToken);
            return obj is null ? StorageStats.DoesNotExist : ToStats(obj);
        }

        protected async Task<IStoragePath> DoResolveAsync(GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken);
            var obj = await Utils.GetAsync(BucketName, GetFullName(subpath), accessToken, cancellationToken);
            return obj is null
                ? (IStoragePath)new GCSStorageFolder(this, in subpath)
                : new GCSStorageRecord(this, in subpath, ToStats(obj));
        }

        protected async Task DoUpdateContentsAsync(
            string fullName,
            Stream contents,
            string? contentType,
            IProgress<StorageOperationProgress>? progress,
            CancellationToken cancellationToken)
        {
            var accessToken = await Driver.GetAccessTokenAsync(cancellationToken);
            var obj0 = await Utils.GetAsync(BucketName, fullName, accessToken, cancellationToken);
            long? total = default;
            Action<long>? onProgress = default;
            if (!(progress is null))
            {
                try
                {
                    total = contents.Length;
                }
                catch { }
                onProgress = (sent) => progress.Report(new StorageOperationProgress(sent, total));
            }
            var obj = await Utils.UploadAsync(
                BucketName,
                fullName,
                contents,
                contentType,
                obj0?.CacheControl,
                obj0?.Acl,
                accessToken,
                onProgress,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var finalSize = unchecked((long)obj.Size!);
            if (!total.HasValue && !(progress is null))
            {
                progress.Report(new StorageOperationProgress(finalSize, finalSize));
            }
        }
    }
}