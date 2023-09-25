using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Storage
{
    public static class ProducerFeatureExtensions
    {
        public static ValueTask<IStreamProducer> CreateRecordProducerAsync(
            this IStorageProvider provider,
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default)
        {
            if (provider is IProducerFeature feature)
            {
                return feature.CreateRecordProducerAsync(in subpath, cancellationToken);
            }
            var subpathCopy = subpath;
            var producer = StreamProducer.Create(async (output, cancellationToken) =>
            {
                await using var input = await provider.CreateReadableStreamAsync(subpathCopy, cancellationToken);
                await input.CopyToAsync(output, 32 * 1024, cancellationToken);
            });
            return new ValueTask<IStreamProducer>(producer);
        }

        public static ValueTask<IStreamProducer> CreateProducerAsync(
            this IStorageRecord record,
            CancellationToken cancellationToken = default)
            => record.Provider.CreateRecordProducerAsync(record.Subpath, cancellationToken);
    }
}