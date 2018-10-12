using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.Features
{
    public interface IRecordCopyFeature
    {
        Task CopyToAsync(IStorageRecord record, Stream destination, int? bufferSize = null, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}