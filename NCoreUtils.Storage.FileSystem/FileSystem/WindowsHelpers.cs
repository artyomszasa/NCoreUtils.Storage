using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace NCoreUtils.Storage.FileSystem
{
    static class WindowsHelpers
    {
        public static List<FileSystemRights> FromOwnerPermissions(StoragePermissions sp)
        {
            var ps = new List<FileSystemRights>();
            if (sp.HasFlag(StoragePermissions.Read))
            {
                ps.Add(FileSystemRights.Read);
            }
            if (sp.HasFlag(StoragePermissions.Write))
            {
                ps.Add(FileSystemRights.Write);
            }
            if (sp.HasFlag(StoragePermissions.Execute))
            {
                ps.Add(FileSystemRights.ExecuteFile);
            }
            return ps;
        }

        public static StoragePermissions GetPermissions(FileSystemAccessRule fileRule)
        {
            var p = StoragePermissions.None;
            if (fileRule.FileSystemRights.HasFlag(FileSystemRights.ExecuteFile))
            {
                p |= StoragePermissions.Execute;
            }
            if (fileRule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute))
            {
                p |= StoragePermissions.Read;
            }

            if (fileRule.FileSystemRights.HasFlag(FileSystemRights.FullControl))
            {
                p |= StoragePermissions.Write;
            }
            return p;
        }
    }
}
