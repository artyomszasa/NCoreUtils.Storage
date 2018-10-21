using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public static class StorageContainerExtensions
    {
        public static IEnumerable<IStorageItem> GetContents(this IStorageContainer storageContainer) => storageContainer.GetContentsAsync().ToEnumerable();

        public static IStorageRecord CreateRecord(this IStorageContainer storageContainer, string name, Stream contents, string contentType = null, IProgress progress = null)
            => storageContainer.CreateRecordAsync(name, contents, contentType, progress).GetAwaiter().GetResult();

        public static IStorageFolder CreateFolder(this IStorageContainer storageContainer, string name, IProgress progress = null)
            => storageContainer.CreateFolderAsync(name, progress).GetAwaiter().GetResult();

        public static async Task<IStorageRecord> CreateRecordAsync(
            this IStorageContainer storageContainer,
            string name,
            byte[] data,
            string contentType = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var buffer = new MemoryStream(data, 0, data.Length, false, true))
            {
                return await storageContainer.CreateRecordAsync(name, buffer, contentType, progress, cancellationToken).ConfigureAwait(false);
            }
        }

        public static IStorageRecord CreateRecord(this IStorageContainer storageContainer, string name, byte[] data, string contentType = null, IProgress progress = null)
            => storageContainer.CreateRecordAsync(name, data, contentType, progress).GetAwaiter().GetResult();
    }
}