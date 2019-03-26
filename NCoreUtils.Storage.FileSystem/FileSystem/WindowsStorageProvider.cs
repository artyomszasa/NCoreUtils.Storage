using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.FileSystem
{
    public class WindowsStorageProvider : StorageProvider
    {
        private readonly FileSystemStorageOptions _options;
        private readonly WindowsStorageRoot _windowsStorageRoot;

        public WindowsStorageProvider(IFeatureCollection<IStorageProvider> features, ILogger<WindowsStorageProvider> logger, IContentAnalyzer contentAnalyzer = null, FileSystemStorageOptions options = null)
      : base(features, logger, contentAnalyzer)
        {
            _options = options ?? new FileSystemStorageOptions { RootPath = @"C:\" };
            _windowsStorageRoot = new WindowsStorageRoot(this, _options.RootPath);
        }

        protected override IEnumerable<StorageRoot> GetFileSystemRoots()
        {
            yield return _windowsStorageRoot;
        }

        protected internal override async Task<StoragePath> ResolvePathAsync(string absolutePath, CancellationToken cancellationToken)
        {
            var path = absolutePath.Trim('\\');
            var fsPath = _windowsStorageRoot.RootPath + path;
            var localPath = FsPath.Parse(path);
            if (Directory.Exists(fsPath))
            {
                return new StorageFolder(_windowsStorageRoot, localPath);
            }
            if (File.Exists(fsPath))
            {
                var mediaType = await _windowsStorageRoot.GetMediaTypeAsync(localPath, cancellationToken).ConfigureAwait(false);
                return new StorageRecord(_windowsStorageRoot, localPath, mediaType);
            }
            return new StoragePath(_windowsStorageRoot, localPath);
        }
    }
}
