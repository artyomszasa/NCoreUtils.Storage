using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class GCSStorageRecord : GCSStoragePath, IStorageRecord
    {
        private volatile StorageStats? _stats;

        private volatile Task<StorageStats>? _pendingStats;

        public GCSStorageRecord(GCSStorageProvider provider, in GenericSubpath subpath, StorageStats? stats = default)
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