using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage
{
    public class ConfiguredStorageProvider<T> : IStorageProvider<T>
    {
        public IStorageProvider Provider { get; }

        public IStorageDriver Driver => Provider.Driver;

        public ConfiguredStorageProvider(IStorageProvider provider)
        {
            Provider = provider;
        }

        public ObservableOperation<IStorageFolder> CreateFolderAsync(in GenericSubpath subpath, IStorageSecurity? acl = null, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            return Provider.CreateFolderAsync(subpath, acl, observeProgress, cancellationToken);
        }

        public ValueTask<Stream> CreateReadableStreamAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            return Provider.CreateReadableStreamAsync(subpath, cancellationToken);
        }

        public ObservableOperation<IStorageRecord> CreateRecordAsync(in GenericSubpath subpath, Stream contents, string? contentType = null, bool @override = true, IStorageSecurity? acl = null, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            return Provider.CreateRecordAsync(subpath, contents, contentType, @override, acl, observeProgress, cancellationToken);
        }

        public ObservableOperation DeleteAsync(in GenericSubpath subpath, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            return Provider.DeleteAsync(subpath, observeProgress, cancellationToken);
        }

        public IAsyncEnumerable<IStorageItem> EnumerateItemsAsync(in GenericSubpath subpath)
        {
            return Provider.EnumerateItemsAsync(subpath);
        }

        public ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            return Provider.GetStatsAsync(subpath, cancellationToken);
        }

        public Uri GetUri(in GenericSubpath subpath)
        {
            return Provider.GetUri(subpath);
        }

        public ObservableOperation<T1> RenameAsync<T1>(in GenericSubpath subpath, string name, bool observeProgress = false, CancellationToken cancellationToken = default) where T1 : IStorageItem
        {
            return Provider.RenameAsync<T1>(subpath, name, observeProgress, cancellationToken);
        }

        public ValueTask<IStoragePath> ResolveAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            return Provider.ResolveAsync(subpath, cancellationToken);
        }

        public ObservableOperation UpdateAclAsync(in GenericSubpath subpath, IStorageSecurity acl, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            return Provider.UpdateAclAsync(subpath, acl, observeProgress, cancellationToken);
        }

        public ObservableOperation UpdateContentsAsync(in GenericSubpath subpath, Stream contents, string? contentType = null, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            return Provider.UpdateContentsAsync(subpath, contents, contentType, observeProgress, cancellationToken);
        }
    }
}