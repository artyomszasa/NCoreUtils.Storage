using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public interface IStorageItem : IStoragePath
    {
        IStorageSecurity Security { get; }

        Task<IStorageContainer> GetContainerAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task DeleteAsync(IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));

        Task UpdateSecurityAsync(IStorageSecurity security, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}