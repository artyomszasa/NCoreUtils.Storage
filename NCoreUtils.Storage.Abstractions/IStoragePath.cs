using System;

namespace NCoreUtils.Storage
{
    public interface IStoragePath
    {
        IStorageRoot StorageRoot { get; }

        Uri Uri { get; }
    }
}