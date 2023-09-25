using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageItem : IStoragePath
    {
        async ValueTask<IStorageContainer?> ResolveContainerAsync(CancellationToken cancellationToken = default)
        {
            if (Subpath.SegmentCount == 0)
            {
                return default;
            }
            var parent = await Provider.ResolveAsync(Subpath.GetParentPath());
            return (IStorageContainer)parent;
        }

        ObservableOperation<IStorageItem> RenameAsync(
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => Provider.RenameAsync<IStorageItem>(Subpath, name, observeProgress, cancellationToken);
    }
}