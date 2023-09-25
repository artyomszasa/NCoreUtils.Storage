using System.Collections.Generic;
using System.IO;
using System.Threading;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageFolder : IStorageItem, IStorageContainer
    {
        IAsyncEnumerable<IStorageItem> IStorageContainer.GetContentsAsync()
            => Provider.EnumerateItemsAsync(Subpath);

        ObservableOperation<IStorageRecord> IStorageContainer.CreateRecordAsync(
            string name,
            Stream contents,
            string? contentType,
            bool @override,
            IStorageSecurity? acl,
            bool observeProgress,
            CancellationToken cancellationToken)
            => Provider.CreateRecordAsync(
                Subpath.Append(name),
                contents,
                contentType,
                @override,
                acl,
                observeProgress,
                cancellationToken
            );

        ObservableOperation<IStorageFolder> IStorageContainer.CreateFolderAsync(
            string name,
            IStorageSecurity? acl,
            bool observeProgress,
            CancellationToken cancellationToken)
            => Provider.CreateFolderAsync(
                Subpath.Append(name),
                acl,
                observeProgress,
                cancellationToken
            );

        new ObservableOperation<IStorageFolder> RenameAsync(
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => Provider.RenameAsync<IStorageFolder>(Subpath, name, observeProgress, cancellationToken);

    }
}