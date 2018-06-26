using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class CacheControlFeature : ICacheControlFeature
    {
        public async Task UpdateCacheControlAsync(IStorageItem item, TimeSpan cacheDuration, bool isPrivate, CancellationToken cancellationToken)
        {
            if (item is StorageFolder)
            {
                return;
            }
            if (item is StorageRecord record)
            {
                var client = await StorageClient.CreateAsync().ConfigureAwait(false);
                var seconds = (long)Math.Round(cacheDuration.TotalSeconds);
                record.GoogleObject.CacheControl = seconds == 0 ? "no-cache, no-store, must-revalidate" : $"{(isPrivate ? "private" : "public")}, max-age={seconds}";
                await client.UpdateObjectAsync(record.GoogleObject, cancellationToken: cancellationToken);
                record.StorageRoot.StorageProvider.Logger.LogInformation(
                    "Successfully set cache-control to \"{0}\" on \"{1}\".",
                    record.GoogleObject.CacheControl,
                    record.Uri);
                return;
            }
            throw new InvalidOperationException($"Unable to set cache control on object of type {item?.GetType()?.FullName}.");
        }
    }
}