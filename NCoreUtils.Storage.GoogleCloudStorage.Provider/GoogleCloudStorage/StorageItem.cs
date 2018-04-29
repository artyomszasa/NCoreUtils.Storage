using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public abstract class StorageItem : StoragePath, IStorageItem
    {
        public string Name => Path.GetFileName(LocalPath);

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
    }
}