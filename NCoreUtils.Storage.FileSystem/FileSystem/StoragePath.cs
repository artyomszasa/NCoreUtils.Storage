using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class StoragePath : IStoragePath
    {
        internal StorageRoot _storageRoot;

        IStorageRoot IStoragePath.StorageRoot => StorageRoot;

        public virtual StorageRoot StorageRoot => _storageRoot;

        /// <summary>
        /// Path relative to the root. Does not include root drive/folder.
        /// </summary>
        public FsPath LocalPath { get; }

        public virtual string Name => LocalPath.Name;

        public virtual ILogger Logger => StorageRoot.Logger;

        internal StoragePath()
        {
            LocalPath = FsPath.Empty;
        }

        public StoragePath(StorageRoot storageRoot, FsPath localPath)
        {
            _storageRoot = storageRoot ?? throw new ArgumentNullException(nameof(storageRoot));
            LocalPath = localPath ?? throw new ArgumentNullException(nameof(localPath));
        }

        public virtual Uri Uri
        {
            get
            {
                var builder = new UriBuilder
                {
                    Scheme = "file",
                    Host = string.Empty,
                    Path = StorageRoot.GetUriPath(this)
                };
                return builder.Uri;
            }
        }

        public virtual async Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var parentLocalPath = LocalPath.SubPath(-1);
            var parentRelativePath = StorageRoot.GetUriPath(parentLocalPath);
            return await StorageRoot.StorageProvider.ResolvePathAsync(parentRelativePath, cancellationToken);
        }
    }
}