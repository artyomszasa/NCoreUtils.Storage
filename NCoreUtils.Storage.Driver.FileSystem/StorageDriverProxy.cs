using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class StorageDriverProxy : IStorageDriver
    {
        /// <summary>
        /// OS-specific driver.
        /// </summary>
        internal IStorageDriver Driver { get; }

        public StorageDriverProxy(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Driver = new UnixStorageDriver(loggerFactory.CreateLogger<UnixStorageDriver>());
            }
            else
            {
                Driver = new WinStorageDriver(loggerFactory.CreateLogger<WinStorageDriver>());
            }
        }

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync()
            => Driver.GetRootsAsync();

        public ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
            => Driver.ResolveAsync(uri, cancellationToken);
    }
}