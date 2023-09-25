namespace NCoreUtils.Storage.FileSystem
{
    public class StorageRecord : StoragePath, IStorageRecord
    {
        public StorageRecord(StorageProvider provider, in GenericSubpath subpath)
            : base(provider, subpath)
        { }
    }
}