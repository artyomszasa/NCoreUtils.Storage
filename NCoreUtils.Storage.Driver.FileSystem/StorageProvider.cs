using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Storage.Tasks;

namespace NCoreUtils.Storage.FileSystem
{
    public class StorageProvider : IStorageProvider
    {
        private const int DefaultBufferSize = 16 * 1024;

        IStorageDriver IStorageProvider.Driver => Driver;

        public FileSystemStorageDriver Driver { get; }

        public string PathPrefix { get; }

        public string UriPathPrefix { get; }

        public char DirectorySeparator { get; }

        public StorageProvider(FileSystemStorageDriver driver, string pathPrefix, string uriPathPrefix, char directorySeparator)
        {
            Driver = driver;
            PathPrefix = pathPrefix;
            UriPathPrefix = uriPathPrefix;
            DirectorySeparator = directorySeparator;
        }

        // FIXME: optimize
        internal string GetFullPath(in GenericSubpath subpath)
        {
            return PathPrefix + subpath.ToString(DirectorySeparator);
        }

        // FIXME: optimize
        internal string GetFullUriPath(in GenericSubpath subpath)
        {
            return UriPathPrefix + subpath.ToString('/');
        }

        private GenericSubpath GetSubpath(string path)
        {
            if (!path.StartsWith(PathPrefix))
            {
                throw new InvalidOperationException($"\"{path}\" cannot be converted to subpath of \"{PathPrefix}\".");
            }
            var index = PathPrefix.Length;
            if (path[index] == DirectorySeparator)
            {
                ++index;
            }
            return GenericSubpath.Parse(path, index);
        }

        // private void UpdateAcl(string path, IStorageSecurity? acl)
        // {
        //     if (!(acl is null))
        //     {
        //         var sec = new FileSecurity(path, AccessControlSections.All);
        //         foreach (var (actor, permissions) in acl)
        //         {
        //             var identity = new NTAccount(actor.ActorType switch
        //             {
        //                 StorageActorType.Authenticated => "Authenticated Users",
        //                 StorageActorType.Public => "Users",
        //                 _ => actor.Id!
        //             });
        //             sec.AddAccessRule(new FileSystemAccessRule(identity, MapStoragePermissions(permissions), AccessControlType.Allow));
        //         }
        //     }
        // }

        public StorageFolder CreateFolder(
            in GenericSubpath subpath,
            IStorageSecurity? acl = null)
        {
            var path = GetFullPath(in subpath);
            var info = Directory.CreateDirectory(path);
            Driver.UpdateAcl(path, acl);
            return new StorageFolder(this, subpath);
        }

        public ObservableOperation<IStorageFolder> CreateFolderAsync(
            in GenericSubpath subpath,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
            => new ObservableOperation<IStorageFolder>(new ValueTask<IStorageFolder>(CreateFolder(in subpath, acl)));

        public ValueTask<Stream> CreateReadableStreamAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            return new ValueTask<Stream>(File.OpenRead(GetFullPath(subpath)));
        }

        public ObservableOperation<IStorageRecord> CreateRecordAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool @override = true,
            IStorageSecurity? acl = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(in subpath);
            return new ObservableOperation<IStorageRecord>(DoCreateRecordAsync(GetFullPath(in subpath), subpath));

            async ValueTask<IStorageRecord> DoCreateRecordAsync(string fullPath, GenericSubpath subpath)
            {
                using var stream = new FileStream(
                    fullPath,
                    @override ? FileMode.Create : FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    DefaultBufferSize,
                    true
                );
                await contents.CopyToAsync(stream, DefaultBufferSize, cancellationToken);
                Driver.UpdateAcl(fullPath, acl);
                return new StorageRecord(this, subpath);
            }
        }

        public ObservableOperation DeleteAsync(in GenericSubpath subpath, bool observeProgress = false, CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(in subpath);
            File.Delete(path);
            return default;
        }

        public IAsyncEnumerable<IStorageItem> EnumerateItemsAsync(in GenericSubpath subpath)
        {
            var path = GetFullPath(in subpath);
            return Directory.EnumerateFileSystemEntries(path)
                .Select(p => new FileInfo(p).Attributes.HasFlag(FileAttributes.Directory)
                    ? (IStorageItem)new StorageFolder(this, GetSubpath(p))
                    : (IStorageItem)new StorageRecord(this, GetSubpath(p)))
                .ToAsyncEnumerable();
        }

        public ValueTask<StorageStats> GetStatsAsync(in GenericSubpath subpath, CancellationToken cancellationToken = default)
        {
            var f = new FileInfo(GetFullPath(in subpath));
            return new ValueTask<StorageStats>(new StorageStats(
                f.Exists,
                f.Exists ? (long?)f.Length : (long?)default,
                default,
                f.CreationTime,
                f.LastWriteTime,
                // FIXME: ACL
                default
            ));
        }

        public Uri GetUri(in GenericSubpath subpath)
            => new UriBuilder
            {
                Scheme = "file",
                Path = GetFullUriPath(subpath)
            }.Uri;

        public ObservableOperation<T> RenameAsync<T>(
            in GenericSubpath subpath,
            string name,
            bool observeProgress = false,
            CancellationToken cancellationToken = default) where T : IStorageItem
        {
            var path = GetFullPath(subpath);
            var folder = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"No containing folder exists for \"{path}\".");
            var newPath = Path.Combine(folder, name);
            if (new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
            {
                Directory.Move(path, newPath);
                return new ObservableOperation<T>(new ValueTask<T>((T)(object)new StorageFolder(this, GetSubpath(newPath))));
            }
            else
            {
                File.Move(path, newPath);
                return new ObservableOperation<T>(new ValueTask<T>((T)(object)new StorageRecord(this, GetSubpath(newPath))));
            }
        }

        public IStoragePath Resolve(in GenericSubpath subpath)
        {
            var path = GetFullPath(in subpath);
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return new StoragePath(this, in subpath);
            }
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                return new StorageFolder(this, in subpath);
            }
            return new StorageRecord(this, in subpath);
        }

        public ValueTask<IStoragePath> ResolveAsync(
            in GenericSubpath subpath,
            CancellationToken cancellationToken = default)
            => new ValueTask<IStoragePath>(Resolve(in subpath));

        public ObservableOperation UpdateAclAsync(
            in GenericSubpath subpath,
            IStorageSecurity acl,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            Driver.UpdateAcl(GetFullPath(subpath), acl);
            return default;
        }

        public ObservableOperation UpdateContentsAsync(
            in GenericSubpath subpath,
            Stream contents,
            string? contentType = null,
            bool observeProgress = false,
            CancellationToken cancellationToken = default)
        {
            return new ObservableOperation(DoUpdateContentsAsync(GetFullPath(subpath), subpath));

            async ValueTask DoUpdateContentsAsync(string fullPath, GenericSubpath subpath)
            {
                using var stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None,
                    DefaultBufferSize,
                    true
                );
                await contents.CopyToAsync(stream, DefaultBufferSize, cancellationToken);
            }
        }
    }
}