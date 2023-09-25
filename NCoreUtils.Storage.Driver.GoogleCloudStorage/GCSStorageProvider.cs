using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public partial class GCSStorageProvider : IStorageProvider
    {
        public ObservableOperation<IStorageFolder> CreateFolderAsync(
            in GenericSubpath subpath,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            return new ObservableOperation<IStorageFolder>(new GCSStorageFolder(this, in subpath));
        }

        public ValueTask<Stream> CreateReadableStreamAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
            => new ValueTask<Stream>(DoCreateReadableStream(BucketName, GetFullName(in subpath), cancellationToken));

        public ObservableOperation<IStorageRecord> CreateRecordAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool @override = true,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            var progress = observeProgress ? new Progress<StorageOperationProgress>() : default;
            return new ObservableOperation<IStorageRecord>(
                DoCreateRecordAsync(
                    subpath,
                    contents,
                    contentType,
                    default,
                    @override,
                    acl,
                    progress,
                    cancellationToken
                ),
                progress
            );
        }

        public ObservableOperation DeleteAsync(in GenericSubpath subpath, bool observeProgress = false, CancellationToken cancellationToken = default)
            => new ObservableOperation(DoDeleteAsync(subpath, cancellationToken));

        public IAsyncEnumerable<IStorageItem> EnumerateItemsAsync(in GenericSubpath subpath)
        {
            var sp = subpath;
            return new DelayedAsyncEnumerable<IStorageItem>(cancellationToken => new ValueTask<IAsyncEnumerable<IStorageItem>>(DoEnumerateItemsAsync(
                sp,
                cancellationToken
            )));
        }

        public ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
            => new ValueTask<StorageStats>(DoGetStatsAsync(GetFullName(in subpath), cancellationToken));

        public Uri GetUri(in GenericSubpath subpath)
            => new UriBuilder
            {
                Scheme = "gs",
                Host = BucketName,
                Path = GetFullName(subpath)
            }.Uri;

        public ObservableOperation<T> RenameAsync<T>(
            in GenericSubpath subpath,
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            where T : IStorageItem
        {
            throw new NotImplementedException();
        }

        public ValueTask<IStoragePath> ResolveAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default)
            => new ValueTask<IStoragePath>(DoResolveAsync(subpath, cancellationToken));

        public ObservableOperation UpdateAclAsync(
            in GenericSubpath subpath,
            IStorageSecurity acl,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ObservableOperation UpdateContentsAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            var progress = observeProgress ? new Progress<StorageOperationProgress>() : default;
            return new ObservableOperation(
                DoUpdateContentsAsync(GetFullName(in subpath), contents, contentType, progress, cancellationToken),
                progress
            );
        }
    }
}