using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage.AzureBlobStorage
{
    public partial class AzStorageProvider : IStorageProvider
    {
        IStorageDriver IStorageProvider.Driver => Driver;

        public ObservableOperation<IStorageFolder> CreateFolderAsync(
            in GenericSubpath subpath,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            return new ObservableOperation<IStorageFolder>(new AzStorageFolder(this, in subpath));
        }

        public ValueTask<Stream> CreateReadableStreamAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default)
            => new ValueTask<Stream>(DoCreateReadableStream(GetFullName(in subpath), cancellationToken));

        public ObservableOperation<IStorageRecord> CreateRecordAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool @override = true,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => new ObservableOperation<IStorageRecord>(
                DoCreateRecordAsync(
                    subpath,
                    contents,
                    contentType,
                    default,
                    @override,
                    acl,
                    cancellationToken
                )
            );

        public ObservableOperation DeleteAsync(in GenericSubpath subpath, bool observeProgress = false, CancellationToken cancellationToken = default)
            => new ObservableOperation(DoDeleteAsync(GetFullName(subpath), cancellationToken));

        public IAsyncEnumerable<IStorageItem> EnumerateItemsAsync(in GenericSubpath subpath)
            => DoEnumerateItemsAsync(GetFullName(subpath), default);

        public ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
            => new ValueTask<StorageStats>(DoGetStatsAsync(GetFullName(in subpath), cancellationToken));

        public Uri GetUri(in GenericSubpath subpath)
            => new UriBuilder
            {
                Scheme = "az",
                Host = ContainerName,
                Path = GetFullName(subpath)
            }.Uri;

        public ObservableOperation<T> RenameAsync<T>(in GenericSubpath subpath, string name, bool observeProgress = false, CancellationToken cancellationToken = default) where T : IStorageItem
            => typeof(IStorageRecord).IsAssignableFrom(typeof(T))
                ? new ObservableOperation<T>(
                    UnsafeTaskCast<IStorageRecord, T>(
                        DoRenameRecordAsync(
                            GetFullName(subpath),
                            subpath.GetParentPath().Append(name),
                            cancellationToken
                        )
                    )
                )
                : throw new NotSupportedException($"Rename not supported for {typeof(T)}.");

        public ValueTask<IStoragePath> ResolveAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
            => new ValueTask<IStoragePath>(DoResolveAsync(subpath, cancellationToken));

        public ObservableOperation UpdateAclAsync(in GenericSubpath subpath, IStorageSecurity acl, bool observeProgress = false, CancellationToken cancellationToken = default)
            => new ObservableOperation(default(ValueTask));

        public ObservableOperation UpdateContentsAsync(in GenericSubpath subpath, Stream contents, string? contentType = null, bool observeProgress = false, CancellationToken cancellationToken = default)
            => new ObservableOperation(DoUpdateRecordAsync(subpath, contents, contentType, cancellationToken));
    }
}