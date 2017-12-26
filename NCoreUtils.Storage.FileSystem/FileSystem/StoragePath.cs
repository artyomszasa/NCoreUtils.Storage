using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class StoragePath : IStoragePath
    {
        IStorageRoot IStoragePath.StorageRoot => StorageRoot;

        public StorageRoot StorageRoot { get; }

        /// <summary>
        /// Path relative to the root. Does not include root drive/folder.
        /// </summary>
        public FsPath LocalPath { get; }

        public ILogger Logger => StorageRoot.Logger;

        public StoragePath(StorageRoot storageRoot, FsPath localPath)
        {
            StorageRoot = storageRoot ?? throw new ArgumentNullException(nameof(storageRoot));
            LocalPath = localPath ?? throw new ArgumentNullException(nameof(localPath));
        }

        public Uri Uri => new UriBuilder { Scheme = "file", Host = string.Empty, Path = StorageRoot.GetUriPath(this) }.Uri;
    }
}