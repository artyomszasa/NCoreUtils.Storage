using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.FileSystem
{
    public class WinStorageDriver : FileSystemStorageDriver
    {
        private static readonly Regex _regexDriveLetter = new Regex("^([A-Z]+):\\?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static FileSystemRights MapStoragePermissions(StoragePermissions permissions)
        {
            FileSystemRights result = default;
            if (permissions.HasFlag(StoragePermissions.Read))
            {
                result |= FileSystemRights.Read;
            }
            if (permissions.HasFlag(StoragePermissions.Write))
            {
                result |= FileSystemRights.Modify;
            }
            if (permissions.HasFlag(StoragePermissions.Execute))
            {
                result |= FileSystemRights.ExecuteFile;
            }
            if (permissions.HasFlag(StoragePermissions.Control))
            {
                result |= FileSystemRights.FullControl;
            }
            return result;
        }

        protected ILogger Logger { get; }

        public WinStorageDriver(ILogger<WinStorageDriver> logger)
        {
            Logger = logger;
        }

        internal IReadOnlyList<WinDriveStorageRoot> GetRoots()
        {
            var drives = Directory.GetLogicalDrives();
            var roots = new List<WinDriveStorageRoot>(drives.Length);
            foreach (var drive in drives)
            {
                var m = _regexDriveLetter.Match(drive);
                if (m.Success)
                {
                    roots.Add(new WinDriveStorageRoot(this, m.Groups[1].Value));
                }
            }
            return roots;
        }

        public override IAsyncEnumerable<IStorageRoot> GetRootsAsync()
            => GetRoots().ToAsyncEnumerable();

        public override async ValueTask<IStoragePath?> ResolveAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (uri.Scheme == "file")
            {
                var subpath = GenericSubpath.Parse(uri.AbsolutePath.Trim('/')).Unshift(out var letter);
                foreach (var root in GetRoots())
                {
                    if (root is WinDriveStorageRoot driveRoot && StringComparer.InvariantCultureIgnoreCase.Equals(driveRoot.DriveLetter, letter))
                    {
                        return await driveRoot.ResolveAsync(subpath, cancellationToken);
                    }
                }
            }
            return default;
        }

        public override void UpdateAcl(string path, IStorageSecurity? acl)
        {
            if (!(acl is null))
            {
                var sec = new FileSecurity(path, AccessControlSections.All);
                foreach (var (actor, permissions) in acl)
                {
                    var identity = new NTAccount(actor.ActorType switch
                    {
                        StorageActorType.Authenticated => "Authenticated Users",
                        StorageActorType.Public => "Users",
                        _ => actor.Id!
                    });
                    sec.AddAccessRule(new FileSystemAccessRule(identity, MapStoragePermissions(permissions), AccessControlType.Allow));
                }
            }
        }
    }
}