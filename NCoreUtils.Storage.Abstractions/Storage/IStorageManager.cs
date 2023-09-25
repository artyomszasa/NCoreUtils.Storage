using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageManager
    {
        IReadOnlyList<IStorageDriver> Drivers { get; }

        ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default);
    }
}