using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class WindowsStorageRoot : StorageRoot
    {
        public string RootPath { get; }

        public override Uri Uri { get; }

        public override string Name => RootPath;


        public WindowsStorageRoot(WindowsStorageProvider storageProvider, string rootPath) : base(storageProvider)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            if (!RootPath.EndsWith(@"\"))
            {
                RootPath += @"\";
            }

            Uri = new Uri($"file://{RootPath.Replace('\\', '/')}");
        }

        public override string GetFullPath(FsPath localPath) => RootPath + localPath.Join("\\");

        public override string GetUriPath(FsPath localPath) => '/' + RootPath.TrimEnd('\\') + '/' + localPath.Join("/");

        protected override IEnumerable<string> GetFileSystemEntries(FsPath localPath)
        {
            string path = null == localPath ? RootPath : GetFullPath(localPath);
            return Directory.EnumerateFileSystemEntries(path);
        }

        internal override StorageSecurity GetSecurity(string absolutePath)
        {
            var security = new FileSecurity(absolutePath,
                AccessControlSections.Owner |
                AccessControlSections.Group |
                AccessControlSections.Access);

            var authorizationRules = security.GetAccessRules(true, true, typeof(NTAccount));

            var owner = security.GetOwner(typeof(NTAccount));
            var group = security.GetGroup(typeof(NTAccount));
            var others = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null)
                 .Translate(typeof(NTAccount));

            var userPermissions = StoragePermissions.None;
            var groupPermissions = StoragePermissions.None;
            var publicPermissions = StoragePermissions.None;

            foreach (AuthorizationRule rule in authorizationRules)
            {
                FileSystemAccessRule fileRule = rule as FileSystemAccessRule;
                if (fileRule != null)
                {
                    if (owner != null && fileRule.IdentityReference == owner)
                    {
                        userPermissions |= WindowsHelpers.GetPermissions(fileRule);
                    }
                    else if (group != null && fileRule.IdentityReference == group)
                    {
                        groupPermissions |= WindowsHelpers.GetPermissions(fileRule);
                    }
                    else if (others != null && fileRule.IdentityReference == others)
                    {
                        publicPermissions |= WindowsHelpers.GetPermissions(fileRule);
                    }
                }
            }
            var builder = ImmutableDictionary.CreateBuilder<StorageActor, StoragePermissions>();
            builder.Add(StorageActor.Public, publicPermissions);
            builder.Add(StorageActor.User(owner.Value), userPermissions);
            builder.Add(StorageActor.Group(group.Value), groupPermissions);
            return new StorageSecurity(builder.ToImmutable());
        }

        internal override void SetSecurity(string absolutePath, IStorageSecurity security)
        {
            var fsecurity = new FileSecurity(absolutePath,
              AccessControlSections.Owner |
              AccessControlSections.Group |
              AccessControlSections.Access);

            var authorizationRules = fsecurity.GetAccessRules(true, true, typeof(NTAccount));

            var owner = fsecurity.GetOwner(typeof(NTAccount));
            var group = fsecurity.GetGroup(typeof(NTAccount));
            var others = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null)
                 .Translate(typeof(NTAccount));

            var ps = new List<(IdentityReference, FileSystemRights)>();
            // FIXME: emit warnings
            foreach (var kv in security)
            {
                var actor = kv.Key;
                var sp = kv.Value;
                switch (actor.ActorType)
                {
                    case StorageActorType.Public:
                        foreach (var p in WindowsHelpers.FromOwnerPermissions(sp))
                        {
                            ps.Add((others, p));
                        }
                        break;
                    case StorageActorType.User when actor.Id == owner.Value:
                        foreach (var p in WindowsHelpers.FromOwnerPermissions(sp))
                        {
                            ps.Add((owner, p));
                        }
                        break;
                    case StorageActorType.Group when actor.Id == group.Value:
                        foreach (var p in WindowsHelpers.FromOwnerPermissions(sp))
                        {
                            ps.Add((group, p));
                        }
                        break;
                    default:
                        // warn
                        break;
                }
            }
            foreach (var p in ps)
            {

                fsecurity.ModifyAccessRule(AccessControlModification.Add,
                    new FileSystemAccessRule(p.Item1, p.Item2, AccessControlType.Allow),
                    out bool modified);
                if (!modified)
                {
                    Logger.LogWarning("Failed to modify {access} access on {file} for {owner}", p.Item2, absolutePath, p.Item1);
                }
            }

        }
    }
}
