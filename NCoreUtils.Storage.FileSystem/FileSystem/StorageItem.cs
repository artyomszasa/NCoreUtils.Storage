using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public abstract class StorageItem : StoragePath, IStorageItem
    {
        protected StorageItem(StorageRoot storageRoot, FsPath localPath) : base(storageRoot, localPath) { }

        public string Name => LocalPath.Name;

        public abstract Task DeleteAsync(IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));

        public Task<IStorageContainer> GetContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            IStorageContainer result;
            if (LocalPath.Count > 1)
            {
                result = new StorageFolder(StorageRoot, LocalPath.SubPath(LocalPath.Count - 1));
            }
            else
            {
                result = StorageRoot;
            }
            return Task.FromResult(result);
        }
    }
}