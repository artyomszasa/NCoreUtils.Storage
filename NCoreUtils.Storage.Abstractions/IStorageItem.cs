using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public interface IStorageItem : IStoragePath
    {
        string Name { get; }

        Task DeleteAsync(IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}