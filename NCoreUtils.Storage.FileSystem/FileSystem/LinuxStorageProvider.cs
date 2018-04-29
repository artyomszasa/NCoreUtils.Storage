using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;

namespace NCoreUtils.Storage.FileSystem
{
    public class LinuxStorageProvider : StorageProvider
    {
        readonly LinuxStorageRoot _linuxStorageRoot;

        readonly FileSystemStorageOptions _options;

        public LinuxStorageProvider(IFeatureCollection<IStorageProvider> features, ILogger<LinuxStorageProvider> logger, IContentAnalyzer contentAnalyzer = null, FileSystemStorageOptions options = null)
            : base(features, logger, contentAnalyzer)
        {
            _options = options ?? new FileSystemStorageOptions { RootPath = "/" };
            _linuxStorageRoot = new LinuxStorageRoot(this, _options.RootPath);
        }

        protected override IEnumerable<StorageRoot> GetFileSystemRoots()
        {
            yield return _linuxStorageRoot;
        }

        protected internal override async Task<StoragePath> ResolvePathAsync(string absolutePath, CancellationToken cancellationToken)
        {
            var path = absolutePath.Trim('/');
            var fsPath = _linuxStorageRoot.RootPath + path;
            var localPath = FsPath.Parse(path);
            if (Directory.Exists(fsPath))
            {
                return new StorageFolder(_linuxStorageRoot, localPath);
            }
            if (File.Exists(fsPath))
            {
                var mediaType = await _linuxStorageRoot.GetMediaTypeAsync(localPath, cancellationToken).ConfigureAwait(false);
                return new StorageRecord(_linuxStorageRoot, localPath, mediaType);
            }
            return new StoragePath(_linuxStorageRoot, localPath);
        }
    }
}