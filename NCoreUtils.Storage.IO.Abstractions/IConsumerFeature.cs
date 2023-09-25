using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Storage
{
    public interface IConsumerFeature
    {
        ValueTask<IStreamConsumer<IStorageRecord>> CreateRecordConsumerAsync(
            in GenericSubpath subpath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default
        );
    }
}