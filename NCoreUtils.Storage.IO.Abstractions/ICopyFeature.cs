using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public interface ICopyFeature
    {
        ValueTask<IStorageRecord> CopyAsync(
            in GenericSubpath sourcePath,
            in GenericSubpath targetPath,
            string? contentType = default,
            bool @override = true,
            IStorageSecurity? acl = default,
            CancellationToken cancellationToken = default
        );
    }
}