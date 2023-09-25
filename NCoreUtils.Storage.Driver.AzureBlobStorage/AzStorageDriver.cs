using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace NCoreUtils.Storage.AzureBlobStorage
{
    public class AzStorageDriver : IStorageDriver
    {
        public BlobServiceClient Client { get; }

        public AzStorageDriver(AzStorageDriverConfiguration? configuration = default)
        {
            var connectionString = configuration?.ConnectionString
                ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            Client = new BlobServiceClient(connectionString);
        }

        protected virtual async IAsyncEnumerable<IStorageRoot> DoGetRootsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach(var item in Client.GetBlobContainersAsync(cancellationToken: cancellationToken))
            {
                if (!item.IsDeleted.HasValue || !item.IsDeleted.Value)
                {
                    yield return new AzStorageRoot(this, item.Name);
                }
            }
        }

        public IAsyncEnumerable<IStorageRoot> GetRootsAsync()
            => DoGetRootsAsync(default);

        public async ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (!(uri is null) && uri.Scheme == "az")
            {
                await foreach (var root in DoGetRootsAsync(cancellationToken))
                {
                    if (((AzStorageRoot)root).ContainerName == uri.Host)
                    {
                        return await root.ResolveAsync(GenericSubpath.Parse(uri.AbsolutePath), cancellationToken);
                    }
                }
            }
            return default;
        }
    }
}