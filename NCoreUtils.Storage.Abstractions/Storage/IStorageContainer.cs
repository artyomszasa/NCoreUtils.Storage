using System.Collections.Generic;
using System.IO;
using System.Threading;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageContainer
    {
        IAsyncEnumerable<IStorageItem> GetContentsAsync();

        ObservableOperation<IStorageRecord> CreateRecordAsync(
            string name,
            Stream contents,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);

        ObservableOperation<IStorageFolder> CreateFolderAsync(
            string name,
            IStorageSecurity? acl = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default);
    }
}