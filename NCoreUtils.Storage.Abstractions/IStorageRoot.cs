namespace NCoreUtils.Storage
{
    public interface IStorageRoot : IStorageContainer, IStoragePath
    {
        IStorageProvider StorageProvider { get; }
    }
}