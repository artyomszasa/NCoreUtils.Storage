using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.Features
{
    public interface ICacheControlFeature
    {
        Task UpdateCacheControlAsync(IStorageItem item, TimeSpan cacheDuration, bool isPrivate = false, CancellationToken cancellationToken = default(CancellationToken));
    }
}