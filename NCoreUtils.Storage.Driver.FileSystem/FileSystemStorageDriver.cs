using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.FileSystem
{
    public abstract class FileSystemStorageDriver : IStorageDriver
    {
        public abstract void UpdateAcl(string path, IStorageSecurity? acl);

        public abstract IAsyncEnumerable<IStorageRoot> GetRootsAsync();

        public abstract ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default);
    }
}