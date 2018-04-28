using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public class StorageRecord : StoragePath, IStorageRecord
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

        public string Name => LocalPath.Name;

        public Task<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult<Stream>(_fileInfo.OpenRead());

        public Task DeleteAsync(IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (null != progress)
            {
                progress.Total = 1;
            }
            var fullPath = StorageRoot.GetFullPath(this);
            try
            {
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

        public Task<IStorageContainer> GetContainerAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IStorageContainer result;
            if (LocalPath.Count > 1)
            {
                result = new StorageFolder(StorageRoot, LocalPath.SubPath(LocalPath.Count - 1));
            }
            else
            {
                result = StorageRoot;
            }
            return Task.FromResult(result);
        }

        public Task<IStorageRecord> RenameAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IStorageRecord>(StorageRoot.RenameRecord(this, name, progress));
        }

        public async Task UpdateContentAsync(Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
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
            MediaType = await StorageRoot.GetMediaTypeAsync(LocalPath, CancellationToken.None).ConfigureAwait(false);
        }
    }
}