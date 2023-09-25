using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.AzureBlobStorage
{
    public class AzStorageFolder : AzStoragePath, IStorageFolder
    {
        private static readonly StorageStats _folderStats = new StorageStats(
            true,
            default,
            default,
            default,
            default,
            default
        );

        public AzStorageFolder(AzStorageProvider provider, in GenericSubpath subpath)
            : base(provider, in subpath)
        { }

        public ValueTask<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
            => new ValueTask<StorageStats>(_folderStats);
    }
}