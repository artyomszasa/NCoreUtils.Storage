using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public static class StorageItemExtensions
    {
        public static void Delete(this IStorageItem storageItem, IProgress progress = null) => storageItem.DeleteAsync(progress).GetAwaiter().GetResult();
    }
}