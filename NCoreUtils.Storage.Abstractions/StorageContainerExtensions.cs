using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public static class StorageContainerExtensions
    {
        public static IEnumerable<IStorageItem> GetContents(this IStorageContainer storageContainer) => storageContainer.GetContentsAsync().ToEnumerable();

        public static IStorageRecord CreateRecord(this IStorageContainer storageContainer, string name, Stream contents, IProgress progress = null)
            => storageContainer.CreateRecordAsync(name, contents, progress).GetAwaiter().GetResult();

        public static IStorageFolder CreateFolder(this IStorageContainer storageContainer, string name, IProgress progress = null)
            => storageContainer.CreateFolderAsync(name, progress).GetAwaiter().GetResult();
    }
}