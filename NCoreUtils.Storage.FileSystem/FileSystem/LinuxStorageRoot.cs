using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mono.Unix.Native;
using NCoreUtils.ContentDetection;

namespace NCoreUtils.Storage.FileSystem
{
    public class LinuxStorageRoot : StorageRoot
    {
        public string RootPath { get; }

        public override Uri Uri { get; }

        public override string Name => "/";

        public LinuxStorageRoot(LinuxStorageProvider storageProvider, string rootPath)
            : base(storageProvider)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            if (!RootPath.EndsWith("/"))
            {
                RootPath += "/";
            }
            Uri = new Uri($"file://{(RootPath == "/" ? RootPath : RootPath.TrimEnd('/'))}");
        }

        internal override StorageSecurity GetSecurity(string absolutePath)
        {
            if (0 != Syscall.stat(absolutePath, out var stat))
            {
                throw new LinuxIOException(Syscall.GetLastError());
            }
            var uid = stat.st_uid;
            var uname = LinuxHelpers.GetUserName(uid);
            var gid = stat.st_gid;
            var gname = LinuxHelpers.GetGroupName(gid);
            var publicPermissions = LinuxHelpers.GetOtherPermissions(stat.st_mode);
            var userPermissions = LinuxHelpers.GetOwnerPermissions(stat.st_mode);
            var groupPermissions = LinuxHelpers.GetGroupPermissions(stat.st_mode);
            var builder = ImmutableDictionary.CreateBuilder<StorageActor, StoragePermissions>();
            builder.Add(StorageActor.Public, publicPermissions);
            builder.Add(StorageActor.User(uname), userPermissions);
            builder.Add(StorageActor.Group(gname), groupPermissions);
            return new StorageSecurity(builder.ToImmutable());
        }

        public override string GetUriPath(FsPath localPath) => RootPath.TrimStart('/') + localPath.Join("/");

        public override string GetFullPath(FsPath localPath) => RootPath + localPath.Join("/");

        protected override IEnumerable<string> GetFileSystemEntries(FsPath localPath)
        {
            string path = null == localPath ? RootPath : GetFullPath(localPath);
            return Directory.EnumerateFileSystemEntries(path);
        }

        internal override void SetSecurity(string absolutePath, IStorageSecurity security)
        {
            if (0 != Syscall.stat(absolutePath, out var stat))
            {
                throw new LinuxIOException(Syscall.GetLastError());
            }
            var uid = stat.st_uid;
            var uname = LinuxHelpers.GetUserName(uid);
            var gid = stat.st_gid;
            var gname = LinuxHelpers.GetGroupName(gid);
            var ps = default(FilePermissions);
            // FIXME: emit warnings
            foreach (var kv in security)
            {
                var actor = kv.Key;
                var sp = kv.Value;
                switch (actor.ActorType)
                {
                    case StorageActorType.Public:
                        ps |= LinuxHelpers.FromOtherPermissions(sp);
                        break;
                    case StorageActorType.User when actor.Id == uname:
                        ps |= LinuxHelpers.FromOwnerPermissions(sp);
                        break;
                    case StorageActorType.Group when actor.Id == gname:
                        ps |= LinuxHelpers.FromGroupPermissions(sp);
                        break;
                    default:
                        // warn
                        break;
                }
            }
            if (0 != Syscall.chmod(absolutePath, ps))
            {
                throw new LinuxIOException(Syscall.GetLastError());
            }
        }
    }
}