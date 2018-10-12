using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    class CreateByPathFeature : ICreateByPathFeature
    {
        public StorageFolder CreateFolder(StoragePath path, IProgress progress = null)
        {
            progress.SetTotal(1);
            progress.SetValue(0);
            var folder = new StorageFolder(path.StorageRoot, path.LocalPath);
            progress.SetValue(1);
            return folder;
        }

        public Task<IStorageFolder> CreateFolderAsync(IStorageProvider storageProvider, IStoragePath path, bool recursive = false, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (path is StoragePath googleStoragePath)
                {
                    return Task.FromResult<IStorageFolder>(CreateFolder(googleStoragePath, progress));
                }
                throw new InvalidOperationException($"Invalid storage path of type {path.GetType()}");
            }
            catch (Exception exn)
            {
                return Task.FromException<IStorageFolder>(exn);
            }
        }

        public async Task<IStorageRecord> CreateRecordAsync(IStorageProvider storageProvider, IStoragePath path, Stream contents, string contentType = null, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (path is StoragePath googleStoragePath)
            {
                return await googleStoragePath.StorageRoot.CreateRecordAsync(googleStoragePath.LocalPath, contents, contentType, progress, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"Invalid storage path of type {path.GetType()}");
        }
    }
}