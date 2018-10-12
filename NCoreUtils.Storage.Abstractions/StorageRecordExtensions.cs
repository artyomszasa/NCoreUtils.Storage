using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage
{
    public static class StorageRecordExtensions
    {
        static async Task<byte[]> ToByteArrayAsync(this Stream stream, CancellationToken cancellationToken)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            // try using length
            (long length, long position)? state;
            try
            {
                state = (stream.Length, stream.Position);
            }
            catch
            {
                state = null;
            }
            if (state.HasValue)
            {
                var ll = state.Value.length;
                var lp = state.Value.position;
                if (ll - lp > (long)int.MaxValue)
                {
                    throw new InvalidOperationException($"Unable to convert stream with length = {ll} to byte array.");
                }
                var total = (int)(ll - lp);
                var buffer = new byte[total];
                var offset = 0;
                while (offset < total)
                {
                    var readOnce = await stream.ReadAsync(buffer, offset, total - offset, cancellationToken);
                    if (0 == readOnce)
                    {
                        throw new EndOfStreamException();
                    }
                    offset += readOnce;
                }
                return buffer;
            }
            using (var buffer = new MemoryStream())
            {
                await stream.CopyToAsync(buffer, 8192, cancellationToken);
                return buffer.ToArray();
            }
        }

        public static Stream CreateReadableStream(this IStorageRecord storageRecord) => storageRecord.CreateReadableStreamAsync().GetAwaiter().GetResult();

        public static void UpdateContent(this IStorageRecord storageRecord, Stream contents, string contentType = null, IProgress progress = null)
            => storageRecord.UpdateContentAsync(contents, contentType, progress).GetAwaiter().GetResult();

        public static IStorageRecord Rename(this IStorageRecord storageRecord, string name, IProgress progress = null) => storageRecord.RenameAsync(name, progress).GetAwaiter().GetResult();

        public static async Task<byte[]> ReadAllBytesAsync(this IStorageRecord record, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var source = await record.CreateReadableStreamAsync(cancellationToken))
            {
                return await source.ToByteArrayAsync(cancellationToken);
            }
        }

        public static byte[] ReadAllBytes(this IStorageRecord record) => record.ReadAllBytesAsync().GetAwaiter().GetResult();

        public static async Task CopyToAsync(
            this IStorageRecord record,
            Stream destination,
            int? bufferSize = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (record.GetStorageProvider().Features.TryGetFeature(out IRecordCopyFeature implementation))
            {
                await implementation.CopyToAsync(record, destination, bufferSize, progress, cancellationToken);
            }
            else
            {
                using (var buffer = await record.CreateReadableStreamAsync(cancellationToken))
                {
                    await buffer.CopyToAsync(destination, bufferSize ?? 8192, progress, cancellationToken);
                    await destination.FlushAsync();
                }
            }
        }
    }
}