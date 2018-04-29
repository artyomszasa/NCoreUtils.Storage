using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public static class StorageItemExtensions
    {
        public static IStorageContainer GetContainer(this IStorageItem storageItem)
            => storageItem.GetContainerAsync().GetAwaiter().GetResult();

        public static void Delete(this IStorageItem storageItem, IProgress progress = null) => storageItem.DeleteAsync(progress).GetAwaiter().GetResult();
    }
}