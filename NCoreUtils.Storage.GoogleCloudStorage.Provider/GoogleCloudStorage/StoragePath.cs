using System;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StoragePath : IStoragePath
    {
        IStorageRoot IStoragePath.StorageRoot => StorageRoot;

        public StorageRoot StorageRoot { get; }

        public Uri Uri => new Uri(StorageRoot.Uri, LocalPath);

        public string LocalPath { get; }

        public StoragePath(StorageRoot storageRoot, string localPath)
        {
            StorageRoot = storageRoot ?? throw new ArgumentNullException(nameof(storageRoot));
            LocalPath = localPath ?? throw new ArgumentNullException(nameof(localPath));
        }
    }
}