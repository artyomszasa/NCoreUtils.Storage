using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    sealed class StorageFolder : StorageItem, IStorageFolder
    {
        public StorageFolder(StorageRoot storageRoot, FsPath localPath) : base(storageRoot, localPath) { }



        public Task<IStorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => StorageRoot.CreateFolderAsync(LocalPath + name, progress, cancellationToken);

        public Task<IStorageRecord> CreateRecordAsync(string name, Stream contents, string contentType = null, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => StorageRoot.CreateRecordAsync(LocalPath + name, contents, contentType, progress, cancellationToken);

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
                Directory.Delete(fullPath, true);
                if (null != progress)
                {
                    progress.Value = 1;
                }
                Logger.LogDebug("Successfully deleted folder \"{0}\".", fullPath);
                return Task.CompletedTask;
            }
            catch (Exception exn)
            {
                Logger.LogError("Failed to delete folder \"{0}\".", fullPath);
                return Task.FromException(exn);
            }
        }

        public IAsyncEnumerable<IStorageItem> GetContentsAsync() => StorageRoot.GetContentsAsync(LocalPath);
    }
}