using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStoragePath
    {
        IStorageProvider Provider { get; }

        ref readonly GenericSubpath Subpath { get; }

        IStorageDriver Driver
            => Provider.Driver;

        ValueTask<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
            => Provider.GetStatsAsync(Subpath, cancellationToken);
    }
}