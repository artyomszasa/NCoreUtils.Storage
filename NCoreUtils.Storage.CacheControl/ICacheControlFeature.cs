using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public interface ICacheControlFeature
    {
        Task UpdateCacheControlAsync(IStorageItem item, TimeSpan cacheDuration, CancellationToken cancellationToken);
    }
}