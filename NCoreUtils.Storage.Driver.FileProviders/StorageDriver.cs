using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.FileProviders
{
    public abstract class StorageDriver : IStorageDriver
    {
        public virtual string UriScheme => "file";

        IAsyncEnumerable<IStorageRoot> IStorageDriver.GetRootsAsync()
            => GetRootsAsync().ToAsyncEnumerable();

        public abstract IEnumerable<StorageRoot> GetRootsAsync();

        public async ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (uri.Scheme == UriScheme)
            {
                foreach (var root in GetRootsAsync())
                {
                    if (root.Name == uri.Host)
                    {
                        return await root.ResolveAsync(GenericSubpath.Parse(uri.AbsolutePath), cancellationToken);
                    }
                }
            }
            return default;
        }
    }
}