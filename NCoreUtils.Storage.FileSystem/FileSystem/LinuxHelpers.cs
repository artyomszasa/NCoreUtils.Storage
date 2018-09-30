using Mono.Unix.Native;

namespace NCoreUtils.Storage.FileSystem
{
    static class LinuxHelpers
    {
        public static string GetUserName(uint uid)
        {
            var passwd = Syscall.getpwuid(uid);
            return passwd?.pw_name ?? $"#{uid}";
        }

        public static string GetGroupName(uint gid)
        {
            var passwd = Syscall.getgrgid(gid);
            return passwd?.gr_name ?? $"#{gid}";
        }

        public static StoragePermissions GetOwnerPermissions(FilePermissions filePermissions)
        {
            var p = StoragePermissions.None;
            if (filePermissions.HasFlag(FilePermissions.S_IRUSR))
            {
                p |= StoragePermissions.Read;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IWUSR))
            {
                p |= StoragePermissions.Write;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IXUSR))
            {
                p |= StoragePermissions.Execute;
            }
            return p;
        }

        public static StoragePermissions GetGroupPermissions(FilePermissions filePermissions)
        {
            var p = StoragePermissions.None;
            if (filePermissions.HasFlag(FilePermissions.S_IRGRP))
            {
                p |= StoragePermissions.Read;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IWGRP))
            {
                p |= StoragePermissions.Write;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IXGRP))
            {
                p |= StoragePermissions.Execute;
            }
            return p;
        }

        public static StoragePermissions GetOtherPermissions(FilePermissions filePermissions)
        {
            var p = StoragePermissions.None;
            if (filePermissions.HasFlag(FilePermissions.S_IROTH))
            {
                p |= StoragePermissions.Read;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IWOTH))
            {
                p |= StoragePermissions.Write;
            }
            if (filePermissions.HasFlag(FilePermissions.S_IXOTH))
            {
                p |= StoragePermissions.Execute;
            }
            return p;
        }

        public static FilePermissions FromOwnerPermissions(StoragePermissions sp)
        {
            var p = default(FilePermissions);
            if (sp.HasFlag(StoragePermissions.Read))
            {
                p |= FilePermissions.S_IRUSR;
            }
            if (sp.HasFlag(StoragePermissions.Write))
            {
                p |= FilePermissions.S_IWUSR;
            }
            if (sp.HasFlag(StoragePermissions.Execute))
            {
                p |= FilePermissions.S_IXUSR;
            }
            return p;
        }

        public static FilePermissions FromGroupPermissions(StoragePermissions sp)
        {
            var p = default(FilePermissions);
            if (sp.HasFlag(StoragePermissions.Read))
            {
                p |= FilePermissions.S_IRGRP;
            }
            if (sp.HasFlag(StoragePermissions.Write))
            {
                p |= FilePermissions.S_IWGRP;
            }
            if (sp.HasFlag(StoragePermissions.Execute))
            {
                p |= FilePermissions.S_IXGRP;
            }
            return p;
        }

        public static FilePermissions FromOtherPermissions(StoragePermissions sp)
        {
            var p = default(FilePermissions);
            if (sp.HasFlag(StoragePermissions.Read))
            {
                p |= FilePermissions.S_IROTH;
            }
            if (sp.HasFlag(StoragePermissions.Write))
            {
                p |= FilePermissions.S_IWOTH;
            }
            if (sp.HasFlag(StoragePermissions.Execute))
            {
                p |= FilePermissions.S_IXOTH;
            }
            return p;
        }
    }
}