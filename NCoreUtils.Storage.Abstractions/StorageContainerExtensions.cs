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

        public static IStorageRecord CreateRecord(this IStorageContainer storageContainer, string name, Stream contents, IProgress progress = null)
            => storageContainer.CreateRecordAsync(name, contents, progress).GetAwaiter().GetResult();

        public static IStorageFolder CreateFolder(this IStorageContainer storageContainer, string name, IProgress progress = null)
            => storageContainer.CreateFolderAsync(name, progress).GetAwaiter().GetResult();

        public static async Task<IStorageRecord> CreateRecordAsync(
            this IStorageContainer storageContainer,
            string name,
            byte[] data,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var buffer = new MemoryStream(data, false))
            {
                return await storageContainer.CreateRecordAsync(name, buffer, progress, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<IStorageRecord> CreateRecordAsync(this IStorageContainer storageContainer, string name, byte[] data, CancellationToken cancellationToken)
            => storageContainer.CreateRecordAsync(name, data, null, cancellationToken);

        public static IStorageRecord CreateRecord(this IStorageContainer storageContainer, string name, byte[] data, IProgress progress = null)
            => storageContainer.CreateRecordAsync(name, data, progress).GetAwaiter().GetResult();
    }
}