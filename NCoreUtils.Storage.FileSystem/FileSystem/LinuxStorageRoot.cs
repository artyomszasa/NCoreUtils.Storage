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
        public string RootPath { get; }

        public override Uri Uri { get; }

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

        public override string GetUriPath(FsPath localPath) => RootPath.TrimStart('/') + localPath.Join("/");

        public override string GetFullPath(FsPath localPath) => RootPath + localPath.Join("/");

        protected override IEnumerable<string> GetFileSystemEntries(FsPath localPath)
        {
            string path = null == localPath ? RootPath : GetFullPath(localPath);
            return Directory.EnumerateFileSystemEntries(path);
        }
    }
}