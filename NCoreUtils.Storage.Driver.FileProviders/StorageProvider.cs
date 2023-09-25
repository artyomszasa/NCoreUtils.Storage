using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage.FileProviders
{
    public class StorageProvider : IStorageProvider
    {
        IStorageDriver IStorageProvider.Driver => throw new NotImplementedException();

        public IFileProvider FileProvider { get; }

        public string Name { get; }

        public StorageDriver Driver { get; }

        public string PathPrefix { get; }

        public StorageProvider(StorageDriver driver, string name, IFileProvider fileProvider, string pathPrefix)
        {
            Driver = driver;
            FileProvider = fileProvider;
            Name = name;
            PathPrefix = pathPrefix;
        }

        // FIXME: optimize
        private string GetFullPath(in GenericSubpath subpath)
        {
            return PathPrefix + subpath.ToString('/');
        }

        private GenericSubpath GetSubpath(string path)
        {
            if (!path.StartsWith(PathPrefix))
            {
                throw new InvalidOperationException($"\"{path}\" cannot be converted to subpath of \"{PathPrefix}\".");
            }
            var index = PathPrefix.Length;
            if (path[index] == '/')
            {
                ++index;
            }
            return GenericSubpath.Parse(path, index);
        }

        private IStorageItem ToStorageItem(in GenericSubpath subpath, IFileInfo info)
        {
            if (info.IsDirectory)
            {
                return new StorageFolder(this, subpath);
            }
            return new StorageRecord(this, subpath);
        }

        public ObservableOperation<IStorageFolder> CreateFolderAsync(
            in GenericSubpath subpath,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");

        public ValueTask<Stream> CreateReadableStreamAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");

        public ObservableOperation<IStorageRecord> CreateRecordAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool @override = true,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");

        public ObservableOperation DeleteAsync(
            in GenericSubpath subpath,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");

        public IEnumerable<IStorageItem> EnumerateItems(GenericSubpath subpath)
        {
            var fullPath = GetFullPath(in subpath);
            foreach (var info in FileProvider.GetDirectoryContents(fullPath))
            {
                if (info.Exists)
                {
                    yield return ToStorageItem(subpath.Append(info.Name), info);
                }
            }
        }

        IAsyncEnumerable<IStorageItem> IStorageProvider.EnumerateItemsAsync(in GenericSubpath subpath)
            => EnumerateItems(subpath).ToAsyncEnumerable();

        public ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(in subpath);
            var info = FileProvider.GetFileInfo(fullPath);
            return new ValueTask<StorageStats>(info.Exists
                ? StorageStats.DoesNotExist
                : new StorageStats(
                    true,
                    info.Length,
                    default,
                    default,
                    info.LastModified,
                    default
                )
            );
        }

        public Uri GetUri(in GenericSubpath subpath)
            => new UriBuilder
            {
                Scheme = Driver.UriScheme,
                Host = Name,
                Path = GetFullPath(in subpath)
            }.Uri;

        public ObservableOperation<T> RenameAsync<T>(in GenericSubpath subpath, string name, bool observeProgress = false, CancellationToken cancellationToken = default) where T : IStorageItem
            => throw new NotSupportedException("Resource based file system is readonly.");

        public ValueTask<IStoragePath> ResolveAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(in subpath);
            var info = FileProvider.GetFileInfo(fullPath);
            if (info.Exists)
            {
                return new ValueTask<IStoragePath>(ToStorageItem(in subpath, info));
            }
            return new ValueTask<IStoragePath>(new StoragePath(this, in subpath));
        }

        public ObservableOperation UpdateAclAsync(in GenericSubpath subpath, IStorageSecurity acl, bool observeProgress = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");

        public ObservableOperation UpdateContentsAsync(in GenericSubpath subpath, Stream contents, string? contentType = null, bool observeProgress = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Resource based file system is readonly.");
    }
}