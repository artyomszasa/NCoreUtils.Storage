using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public interface IStorageRecord : IStorageItem
    {
        long Size { get; }

        string MediaType { get; }

        Task<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task UpdateContentAsync(Stream contents, string contentType = null, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<IStorageRecord> RenameAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}