namespace NCoreUtils.Storage.FileSystem
{
    public class StorageFolder : StoragePath, IStorageFolder
    {
        public StorageFolder(StorageProvider provider, in GenericSubpath subpath)
            : base(provider, subpath)
        { }
    }
}