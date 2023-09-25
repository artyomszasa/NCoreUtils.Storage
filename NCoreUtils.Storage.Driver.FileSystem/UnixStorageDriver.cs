using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class UnixStorageDriver : FileSystemStorageDriver
    {
        protected ILogger Logger { get; }

        protected internal UnixStorageProvider Root { get; }

        public UnixStorageDriver(ILogger<UnixStorageDriver> logger)
        {
            Logger = logger;
            Root = new UnixStorageProvider(this);
        }

        public override IAsyncEnumerable<IStorageRoot> GetRootsAsync()
        {
            return new IStorageRoot[] { Root }.ToAsyncEnumerable();
        }

        public override async ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (uri.Scheme == "file")
            {
                return await Root.ResolveAsync(GenericSubpath.Parse(uri.AbsolutePath.Trim('/')), cancellationToken);
            }
            return default;
        }

        public override void UpdateAcl(string path, IStorageSecurity? acl)
        {
            if (!(acl is null))
            {
                foreach (var (actor, permissions) in acl)
                {
                    var fileInfo = new Mono.Unix.UnixFileInfo(path);
                    Mono.Unix.FileAccessPermissions perms = default;
                    if (actor.ActorType == StorageActorType.Public)
                    {
                        if (permissions.HasFlag(StoragePermissions.Read))
                        {
                            perms |= (Mono.Unix.FileAccessPermissions.OtherRead & Mono.Unix.FileAccessPermissions.GroupRead & Mono.Unix.FileAccessPermissions.UserRead);
                        }
                        if (permissions.HasFlag(StoragePermissions.Write))
                        {
                            perms |= (Mono.Unix.FileAccessPermissions.OtherWrite & Mono.Unix.FileAccessPermissions.GroupWrite & Mono.Unix.FileAccessPermissions.UserWrite);
                        }
                        if (permissions.HasFlag(StoragePermissions.Execute))
                        {
                            perms |= (Mono.Unix.FileAccessPermissions.OtherExecute & Mono.Unix.FileAccessPermissions.GroupExecute & Mono.Unix.FileAccessPermissions.UserExecute);
                        }
                    }
                    if (actor.ActorType == StorageActorType.User)
                    {
                        if (permissions.HasFlag(StoragePermissions.Read))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.UserRead;
                        }
                        if (permissions.HasFlag(StoragePermissions.Write))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.UserWrite;
                        }
                        if (permissions.HasFlag(StoragePermissions.Execute))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.UserExecute;
                        }
                    }
                    if (actor.ActorType == StorageActorType.Group)
                    {
                        if (permissions.HasFlag(StoragePermissions.Read))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.GroupRead;
                        }
                        if (permissions.HasFlag(StoragePermissions.Write))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.GroupWrite;
                        }
                        if (permissions.HasFlag(StoragePermissions.Execute))
                        {
                            perms |= Mono.Unix.FileAccessPermissions.GroupExecute;
                        }
                    }
                    fileInfo.FileAccessPermissions = perms;
                }
            }
        }
    }
}