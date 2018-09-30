using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public abstract class StorageItem : StoragePath, IStorageItem
    {
        public abstract IStorageSecurity Security { get; }

        public StorageItem(StorageRoot storageRoot, string localPath) : base(storageRoot, localPath) { }

        public virtual Task DeleteAsync(IProgress progress, CancellationToken cancellationToken)
        {
            return StorageRoot.DeleteRecursiveAsync(this, progress, cancellationToken);
        }

        public Task<IStorageContainer> GetContainerAsync(CancellationToken cancellationToken)
        {
            var p = LocalPath.TrimStart('/');
            var i = p.LastIndexOf('/');
            IStorageContainer result;
            if (-1 == i)
            {
                result = StorageRoot;
            }
            else
            {
                result = new StorageFolder(StorageRoot, p.Substring(0, i));
            }
            return Task.FromResult(result);
        }

        public override async Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetContainerAsync(cancellationToken);
        }

        public abstract Task UpdateSecurityAsync(IStorageSecurity security, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}