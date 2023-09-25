using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public class CompositeStoreageDriver : IStorageDriver
    {
        private readonly IReadOnlyList<IStorageDriver> _storageDrivers;

        public CompositeStoreageDriver(IReadOnlyList<IStorageDriver> storageDrivers)
        {
            _storageDrivers = storageDrivers ?? throw new ArgumentNullException(nameof(storageDrivers));
        }

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync()
            => _storageDrivers.ToAsyncEnumerable()
                .SelectMany(driver => driver.GetRootsAsync());

        public async ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            foreach (var driver in _storageDrivers)
            {
                var path = await driver.ResolveAsync(uri, cancellationToken);
                if (!(path is null))
                {
                    return path;
                }
            }
            return default;
        }
    }
}