using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Storage
{
    public static class ConsumerFeatureExtensions
    {
        public static ValueTask<IStreamConsumer<IStorageRecord>> CreateRecordConsumerAsync(
            this IStorageProvider provider,
            in GenericSubpath subpath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default)
        {
            if (provider is IConsumerFeature feature)
            {
                return feature.CreateRecordConsumerAsync(in subpath, contentType, @override, acl, cancellationToken);
            }
            var subpathCopy = subpath;
            var consumer = StreamConsumer.Create<IStorageRecord>((input, cancellationToken) =>
            {
                return provider.CreateRecordAsync(subpathCopy, input, contentType, @override, acl, false, cancellationToken).Task;
            });
            return new ValueTask<IStreamConsumer<IStorageRecord>>(consumer);
        }
    }
}