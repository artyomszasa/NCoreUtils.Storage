using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public abstract class StorageItem : StoragePath, IStorageItem
    {
        public IStorageSecurity Security { get; private set; }

        protected StorageItem(StorageRoot storageRoot, FsPath localPath)
            : base(storageRoot, localPath)
        {
            Security = storageRoot.GetSecurity(localPath);
        }


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

        public override async Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken))
            => await GetContainerAsync(cancellationToken);

        public Task UpdateSecurityAsync(IStorageSecurity security, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (null != progress)
                {
                    progress.Total = 1;
                    progress.Value = 0;
                }
                StorageRoot.SetSecurity(LocalPath, security);
                Security = security;
                return Task.CompletedTask;
            }
            catch (Exception exn)
            {
                return Task.FromException(exn);
            }
            finally
            {
                if (null != progress)
                {
                    progress.Value = 1;
                }
            }
        }
    }
}