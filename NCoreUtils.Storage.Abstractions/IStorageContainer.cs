using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage
{
    public interface IStorageContainer
    {
        IAsyncEnumerable<IStorageItem> GetContentsAsync();

        Task<IStorageRecord> CreateRecordAsync(string name, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<IStorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}