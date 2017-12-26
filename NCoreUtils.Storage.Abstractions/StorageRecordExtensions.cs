using System.IO;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public static class StorageRecordExtensions
    {
        public static Stream CreateReadableStream(this IStorageRecord storageRecord) => storageRecord.CreateReadableStreamAsync().GetAwaiter().GetResult();

        public static void UpdateContent(this IStorageRecord storageRecord, Stream contents, IProgress progress = null)
            => storageRecord.UpdateContentAsync(contents, progress).GetAwaiter().GetResult();

        public static IStorageRecord Rename(this IStorageRecord storageRecord, string name, IProgress progress = null) => storageRecord.RenameAsync(name, progress).GetAwaiter().GetResult();
    }
}