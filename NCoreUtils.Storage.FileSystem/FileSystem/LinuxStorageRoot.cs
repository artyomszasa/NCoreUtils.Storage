using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Linq;

namespace NCoreUtils.Storage.FileSystem
{
    public class LinuxStorageRoot : StorageRoot
    {
        public override Uri Uri { get; } = new Uri("file:///");

        public LinuxStorageRoot(LinuxStorageProvider storageProvider) : base(storageProvider) { }

        public override string GetUriPath(FsPath localPath) => localPath.Join("/");

        public override string GetFullPath(FsPath localPath) => "/" + localPath.Join("/");

        protected override IEnumerable<string> GetFileSystemEntries(FsPath localPath)
        {
            string path = null == localPath ? "/" : GetFullPath(localPath);
            return Directory.EnumerateFileSystemEntries(path);
        }
    }
}