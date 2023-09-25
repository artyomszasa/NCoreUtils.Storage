namespace NCoreUtils.Storage.FileProviders
{
    public class StorageRecord : StoragePath, IStorageRecord
    {
        public StorageRecord(StorageProvider provider, in GenericSubpath subpath)
            : base(provider, subpath)
        { }
    }
}