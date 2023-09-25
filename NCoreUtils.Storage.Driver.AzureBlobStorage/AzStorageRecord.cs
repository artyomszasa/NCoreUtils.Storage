using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.AzureBlobStorage
{
    public class AzStorageRecord : AzStoragePath, IStorageRecord
    {
        private volatile StorageStats? _stats;

        private volatile Task<StorageStats>? _pendingStats;

        public AzStorageRecord(AzStorageProvider provider, in GenericSubpath subpath, StorageStats? stats = default)
            : base(provider, in subpath)
        {
            _stats = stats;
        }

        ValueTask<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            if (!(_stats is null))
            {
                return new ValueTask<StorageStats>(_stats);
            }
            if (_pendingStats is null)
            {
                _pendingStats = Provider.GetStatsAsync(in Subpath, cancellationToken).AsTask();
                _pendingStats.ContinueWith(t =>
                {
                    _stats = t.Result;
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            return new ValueTask<StorageStats>(_pendingStats);
        }
    }
}