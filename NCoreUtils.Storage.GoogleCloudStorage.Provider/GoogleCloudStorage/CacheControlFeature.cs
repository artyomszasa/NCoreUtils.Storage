using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class CacheControlFeature : ICacheControlFeature
    {
        public Task UpdateCacheControlAsync(IStorageItem item, TimeSpan cacheDuration, bool isPrivate, CancellationToken cancellationToken)
        {
            try
            {
                if (item is StorageFolder)
                {
                    return Task.CompletedTask;
                }
                if (item is StorageRecord record)
                {
                    return record.StorageRoot.UseStorageClient(async client =>
                    {
                        var seconds = (long)Math.Round(cacheDuration.TotalSeconds);
                        record.GoogleObject.CacheControl = seconds == 0 ? "no-cache, no-store, must-revalidate" : $"{(isPrivate ? "private" : "public")}, max-age={seconds}";
                        await client.UpdateObjectAsync(record.GoogleObject, cancellationToken: cancellationToken).ConfigureAwait(false);
                        record.StorageRoot.StorageProvider.Logger.LogInformation(
                            "Successfully set cache-control to \"{0}\" on \"{1}\".",
                            record.GoogleObject.CacheControl,
                            record.Uri);
                    });
                }
                throw new InvalidOperationException($"Unable to set cache control on object of type {item?.GetType()?.FullName}.");
            }
            catch (Exception exn)
            {
                return Task.FromException(exn);
            }
        }
    }
}