using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public abstract class StoragePath : IStoragePath
    {
        IStorageRoot IStoragePath.StorageRoot => StorageRoot;

        public StorageRoot StorageRoot { get; internal set; }

        public virtual Uri Uri => new Uri(StorageRoot.Uri, LocalPath);

        public virtual string Name => System.IO.Path.GetFileName(LocalPath);

        public string LocalPath { get; }

        public StoragePath(StorageRoot storageRoot, string localPath)
        {
            StorageRoot = storageRoot ?? throw new ArgumentNullException(nameof(storageRoot));
            LocalPath = localPath ?? throw new ArgumentNullException(nameof(localPath));
        }

        internal StoragePath()
        {
            LocalPath = "/";
        }

        // public virtual async Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken))
        // {
        //     var p = LocalPath.TrimStart('/');
        //     var lastSep = p.LastIndexOf('/');
        //     if (-1 == lastSep)
        //     {
        //         return StorageRoot;
        //     }
        //     var newLocalPath = p.Substring(0, lastSep);
        //     var newUri = new Uri(StorageRoot.Uri, newLocalPath);
        //     return await StorageRoot.StorageProvider.ResolveAsync(newUri, cancellationToken);
        // }

        public abstract Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}