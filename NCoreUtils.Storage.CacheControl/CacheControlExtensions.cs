using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public static class CacheControlExtensions
    {
        public static Task SetCacheControlAsync(this IStorageItem item, TimeSpan cacheDuration, bool isPrivate = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var provider = item.StorageRoot.StorageProvider;
            if (provider.Features.TryGetFeature(out ICacheControlFeature feature))
            {
                return feature.UpdateCacheControlAsync(item, cacheDuration, isPrivate, cancellationToken);
            }
            throw new InvalidOperationException($"Provider {provider.GetType().FullName} does not support cache control feature.");
        }
    }
}