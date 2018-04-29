using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using NCoreUtils.Linq;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageFolder : StorageItem, IStorageFolder
    {
        public StorageFolder(StorageRoot storageRoot, string localPath) : base(storageRoot, localPath) { }

        IAsyncEnumerable<IStorageItem> IStorageContainer.GetContentsAsync() => GetContentsAsync();

        async Task<IStorageFolder> IStorageContainer.CreateFolderAsync(string name, IProgress progress, CancellationToken cancellationToken)
            => await CreateFolderAsync(name, progress, cancellationToken);

        async Task<IStorageRecord> IStorageContainer.CreateRecordAsync(string name, Stream contents, IProgress progress, CancellationToken cancellationToken)
            => await CreateRecordAsync(name, contents, progress, cancellationToken);

        public Task<StorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => StorageRoot.CreateFolderAsync($"{LocalPath}/{name}", progress, cancellationToken);

        public Task<StorageRecord> CreateRecordAsync(string name, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => StorageRoot.CreateRecordAsync($"{LocalPath}/{name}", contents, progress, cancellationToken);

        public IAsyncEnumerable<StorageItem> GetContentsAsync()
            => DelayedAsyncEnumerable.Delay(cancellationToken => StorageRoot.GetContentsAsync(LocalPath, cancellationToken));
    }
}