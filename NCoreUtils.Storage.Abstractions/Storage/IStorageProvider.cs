using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageProvider : IStorageContainer
    {
        IStorageDriver Driver { get; }

        Uri GetUri(in GenericSubpath subpath);

        /// <summary>
        /// Retrieves <see cref="Uri" /> that represent the resource spcified by <paramref name="item" />. In
        /// opposite to the <see cref="IStorageProvider.GetUri(in GenericSubpath)" /> encapsulates access-related
        /// information in the returned <see cref="Uri" /> which allows authorized access to the resource outside the
        /// current authorization context.
        /// </summary>
        /// <param name="item">Resource.</param>
        /// <param name="access">Required access.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="Uri" /> representing the resource with authroization information.</returns>
        ValueTask<Uri> SerializeAsync(
            IStorageItem item,
            StoragePermissions access = StoragePermissions.Read,
            CancellationToken cancellationToken = default)
            => new ValueTask<Uri>(GetUri(in item.Subpath));

        ObservableOperation<IStorageFolder> CreateFolderAsync(
            in GenericSubpath subpath,
            IStorageSecurity? acl = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);

        ObservableOperation<IStorageFolder> IStorageContainer.CreateFolderAsync(
            string name,
            IStorageSecurity? acl,
            bool observeProgress,
            CancellationToken cancellationToken)
            => CreateFolderAsync(GenericSubpath.Parse(name), acl, observeProgress, cancellationToken);

        ObservableOperation<IStorageRecord> CreateRecordAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);

        ObservableOperation<IStorageRecord> IStorageContainer.CreateRecordAsync(
            string name,
            Stream contents,
            string? contentType,
            bool @override,
            IStorageSecurity? acl,
            bool observeProgress,
            CancellationToken cancellationToken)
            => CreateRecordAsync(
                GenericSubpath.Parse(name),
                contents,
                contentType,
                @override,
                acl,
                observeProgress,
                cancellationToken);

        ValueTask<Stream> CreateReadableStreamAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default);

        ObservableOperation DeleteAsync(
            in GenericSubpath subpath,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<IStorageItem> EnumerateItemsAsync(in GenericSubpath subpath);

        IAsyncEnumerable<IStorageItem> IStorageContainer.GetContentsAsync()
            => EnumerateItemsAsync(in GenericSubpath.Empty);

        ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default);

        ObservableOperation<T> RenameAsync<T>(
            in GenericSubpath subpath,
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            where T : IStorageItem;

        ValueTask<IStoragePath> ResolveAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default);

        ObservableOperation UpdateAclAsync(
            in GenericSubpath subpath,
            IStorageSecurity acl,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);

        ObservableOperation UpdateContentsAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);
    }

    public interface IStorageProvider<T> : IStorageProvider { }
}