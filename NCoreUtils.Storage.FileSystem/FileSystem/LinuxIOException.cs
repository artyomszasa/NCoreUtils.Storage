using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Mono.Unix.Native;

namespace NCoreUtils.Storage.FileSystem
{
    [Serializable]
    public class LinuxIOException : IOException
    {
        const string KeyHasPath = "IOHasPath";
        const string KeyPath = "IOPath";

        static readonly ImmutableDictionary<Errno, string> _defaultMessages = new Dictionary<Errno, string>
        {
            { Errno.EACCES, "Search permission is denied for one of the directories in the path prefix." },
            { Errno.EBADF, "Not a valid open file descriptor." },
            { Errno.EFAULT, "Bad address." },
            { Errno.ELOOP, "Too many symbolic links encountered while traversing the path." },
            { Errno.ENAMETOOLONG, "Path is too long." },
            { Errno.ENAMETOOLONG, "A component of path does not exist, or path is an empty string and AT_EMPTY_PATH was not specified in flags." },
            { Errno.ENOMEM, "Out of memory." },
            { Errno.ENOTDIR, "A component of the path prefix of path is not a directory." },
            { Errno.EINVAL, "Path refers to a file whose size, inode number, or number of blocks cannot be represented." }
        }.ToImmutableDictionary();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string GetDefaultMessage(Errno errno)
        {
            if (_defaultMessages.TryGetValue(errno, out var message))
            {
                return message;
            }
            return "IO error occured.";
        }

        public string Path { get; }

        public override string Message
        {
            get
            {
                var message = base.Message;
                if (string.IsNullOrEmpty(Path))
                {
                    return message;
                }
                return null == message || !message.EndsWith(".") ? message : $"{message.Substring(0, message.Length - 1)} [path = {Path}].";
            }
        }

        public LinuxIOException(string message, Errno errno, string path = null)
            : base(message, (int)errno)
        {
            Path = path;
        }

        public LinuxIOException(Errno errno, string path = null) : this(GetDefaultMessage(errno), errno, path) { }

        protected LinuxIOException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Path = info.GetBoolean(KeyHasPath) ? info.GetString(KeyPath) : null;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            var hasPath = null == Path;
            info.AddValue(KeyHasPath, hasPath);
            if (hasPath)
            {
                info.AddValue(KeyPath, Path);
            }
        }
    }
}