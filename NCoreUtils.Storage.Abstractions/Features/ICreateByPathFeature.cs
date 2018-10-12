using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.Features
{
    public interface ICreateByPathFeature
    {
        Task<IStorageFolder> CreateFolderAsync(
            IStorageProvider storageProvider,
            IStoragePath path,
            bool recursive = false,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<IStorageRecord> CreateRecordAsync(
            IStorageProvider storageProvider,
            IStoragePath path,
            Stream contents,
            string contentType = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}