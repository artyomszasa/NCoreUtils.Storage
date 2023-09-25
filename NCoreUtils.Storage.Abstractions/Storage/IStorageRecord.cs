using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStorageRecord : IStorageItem
    {
        ValueTask<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken = default)
            => Provider.CreateReadableStreamAsync(Subpath, cancellationToken);

        ObservableOperation UpdateContentsAsync(
            Stream contents,
            string? contentType = default,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => Provider.UpdateContentsAsync(
                Subpath,
                contents,
                contentType,
                observeProgress,
                cancellationToken
            );

        new ObservableOperation<IStorageRecord> RenameAsync(
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => Provider.RenameAsync<IStorageRecord>(Subpath, name, observeProgress, cancellationToken);
    }
}