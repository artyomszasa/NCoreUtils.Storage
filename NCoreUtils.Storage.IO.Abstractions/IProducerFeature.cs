using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Storage
{
    public interface IProducerFeature
    {
        ValueTask<IStreamProducer> CreateRecordProducerAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default
        );
    }
}