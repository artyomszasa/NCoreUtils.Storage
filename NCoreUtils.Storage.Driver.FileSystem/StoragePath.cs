using System;

namespace NCoreUtils.Storage.FileSystem
{
    public class StoragePath : IStoragePath
    {
        private readonly GenericSubpath _subpath;

        IStorageProvider IStoragePath.Provider => Provider;

        public StorageProvider Provider { get; }

        public ref readonly GenericSubpath Subpath => ref _subpath;

        public StoragePath(StorageProvider provider, in GenericSubpath subpath)
        {
            _subpath = subpath;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
    }
}