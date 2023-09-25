using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class GCSStorageDriver : IStorageDriver
    {

        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}