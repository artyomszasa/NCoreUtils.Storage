using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public class StorageRecord : StorageItem, IStorageRecord
    {
        internal readonly FileInfo _fileInfo;

        public StorageRecord(StorageRoot storageRoot, FsPath localPath, string mediaType)
            : base(storageRoot, localPath)
        {
            _fileInfo = new FileInfo(storageRoot.GetFullPath(localPath));
            MediaType = mediaType;
        }

        public long Size => _fileInfo.Length;

        public string MediaType { get; private set; }


        public Task<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult<Stream>(_fileInfo.OpenRead());

        public override Task DeleteAsync(IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (null != progress)
            {
                progress.Total = 1;
            }
            var fullPath = StorageRoot.GetFullPath(this);
            try
            {
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Unable to delete file as it does not exist.", fullPath);
                }
                File.Delete(fullPath);
                if (null != progress)
                {
                    progress.Value = 1;
                }
                Logger.LogDebug("Successfully deleted file \"{0}\".", fullPath);
                return Task.CompletedTask;
            }
            catch (Exception exn)
            {
                Logger.LogError("Failed to delete file \"{0}\".", fullPath);
                return Task.FromException(exn);
            }
        }

        public Task<IStorageRecord> RenameAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IStorageRecord>(StorageRoot.RenameRecord(this, name, progress));
        }

        public async Task UpdateContentAsync(Stream contents, string contentType = null, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = StorageRoot.GetFullPath(LocalPath);
            try
            {
                if (null != progress)
                {
                    progress.Total = contents.Length;
                }
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, StorageRoot.CopyBufferSize))
                {
                    await contents.CopyToAsync(fileStream, StorageRoot.CopyBufferSize, progress, cancellationToken).ConfigureAwait(false);
                    await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                Logger.LogDebug("Successfully updated file \"{0}\".", fullPath);
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Failed to update file \"{0}\".", fullPath);
                throw;
            }
            MediaType = contentType ?? await StorageRoot.GetMediaTypeAsync(LocalPath, CancellationToken.None).ConfigureAwait(false);
        }
    }
}