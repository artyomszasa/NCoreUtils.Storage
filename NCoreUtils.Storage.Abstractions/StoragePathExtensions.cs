namespace NCoreUtils.Storage
{
    public static class StoragePathExtensions
    {
        public static bool IsRecord(this IStoragePath storagePath) => storagePath is IStorageRecord;

        public static IStorageProvider GetStorageProvider(this IStoragePath storagePath) => storagePath.StorageRoot.StorageProvider;
    }
}