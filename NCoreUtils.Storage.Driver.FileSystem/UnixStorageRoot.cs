namespace NCoreUtils.Storage.FileSystem
{
    public class UnixStorageProvider : StorageProvider, IStorageRoot
    {
        public UnixStorageProvider(FileSystemStorageDriver driver)
            : base(driver, "/", "/", '/')
        { }
    }
}