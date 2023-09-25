using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Storage
{
    public static class CopyFeatureExtensions
    {
        private static async ValueTask<IStorageRecord> FallbackCopyAsync(
            this IStorageProvider provider,
            GenericSubpath sourcePath,
            GenericSubpath targetPath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default)
        {
            await using var producer = await provider.CreateRecordProducerAsync(in sourcePath, cancellationToken)
                .ConfigureAwait(false);
            await using var consumer = await provider.CreateRecordConsumerAsync(
                in targetPath,
                contentType,
                @override,
                acl,
                cancellationToken
            ).ConfigureAwait(false);
            return await producer.ConsumeAsync(consumer, cancellationToken).ConfigureAwait(false);
        }


        public static ValueTask<IStorageRecord> CopyAsync(
            this IStorageProvider provider,
            in GenericSubpath sourcePath,
            in GenericSubpath targetPath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default)
        {
            if (provider is ICopyFeature feature)
            {
                return feature.CopyAsync(in sourcePath, in targetPath, contentType, @override, acl, cancellationToken);
            }
            return FallbackCopyAsync(
                provider,
                sourcePath,
                targetPath,
                contentType,
                @override,
                acl,
                cancellationToken
            );
        }

        public static ValueTask<IStorageRecord> CopyAsync(
            this IStorageRecord record,
            in GenericSubpath targetPath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default)
            => record.Provider.CopyAsync(
                in record.Subpath,
                in targetPath,
                contentType,
                @override,
                acl,
                cancellationToken
            );
    }
}