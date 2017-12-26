using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Linq;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public abstract class StorageRoot : IStorageRoot
    {
        public const int CopyBufferSize = 8192;

        IStorageProvider IStorageRoot.StorageProvider => StorageProvider;

        IStorageRoot IStoragePath.StorageRoot => this;

        public ILogger Logger => StorageProvider.Logger;

        public IContentAnalyzer ContentAnalyzer => StorageProvider.ContentAnalyzer;

        public StorageProvider StorageProvider { get; }

        public abstract Uri Uri { get; }

        public StorageRoot(StorageProvider storageProvider)
            => StorageProvider = storageProvider ?? throw new System.ArgumentNullException(nameof(storageProvider));

        internal virtual async Task<IStorageRecord> CreateRecordAsync(FsPath localPath, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = GetFullPath(localPath);
            try
            {
                if (null != progress)
                {
                    progress.Total = contents.Length;
                }
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize))
                {
                    await contents.CopyToAsync(fileStream, CopyBufferSize, progress, cancellationToken).ConfigureAwait(false);
                    await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                Logger.LogDebug("Successfully created file \"{0}\".", fullPath);
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Failed to create file \"{0}\".", fullPath);
                throw;
            }
            string mediaType = await GetMediaTypeAsync(localPath, cancellationToken).ConfigureAwait(false);
            return new StorageRecord(this, localPath, mediaType);
        }

        internal virtual Task<IStorageFolder> CreateFolderAsync(FsPath localPath, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = GetFullPath(localPath);
            if (null != progress)
            {
                progress.Total = 1;
            }
            try
            {
                Directory.CreateDirectory(fullPath);
                if (null != progress)
                {
                    progress.Value = 1;
                }
                Logger.LogDebug("Successfully created folder \"{0}\".", fullPath);
                return Task.FromResult<IStorageFolder>(new StorageFolder(this, localPath));
            }
            catch (Exception exn)
            {
                Logger.LogDebug(exn, "Failed to create folder \"{0}\".", fullPath);
                return Task.FromException<IStorageFolder>(exn);
            }
        }

        internal IAsyncEnumerable<IStorageItem> GetContentsAsync(FsPath localPath)
            => GetFileSystemEntries(localPath)
                .ToAsyncEnumerable()
                .SelectAsync((path, cancellationToken) => StorageProvider.ResolvePathAsync(path, cancellationToken))
                .Select(boxed => (IStorageItem)boxed);

        /// <summary>
        /// Gets string that can be used as path parameter for <see cref="System.UriBuilder" />.
        /// </summary>
        /// <param name="localPath">Local path to use as source.</param>
        /// <returns>
        /// String that can be used as path parameter for <see cref="System.UriBuilder" />.
        /// </returns>
        public abstract string GetUriPath(FsPath localPath);

        /// <summary>
        /// Gets string that can be used as path parameter for <see cref="System.UriBuilder" />.
        /// </summary>
        /// <param name="storagePath">Storage path to use as source.</param>
        /// <returns>
        /// String that can be used as path parameter for <see cref="System.UriBuilder" />.
        /// </returns>
        public virtual string GetUriPath(StoragePath storagePath) => GetUriPath(storagePath.LocalPath);

        /// <summary>
        /// Gets full path.
        /// </summary>
        /// <param name="localPath">Local path to use as source.</param>
        /// <returns>Full path.</returns>
        public abstract string GetFullPath(FsPath localPath);

        /// <summary>
        /// Gets full path.
        /// </summary>
        /// <param name="storagePath">Storage path to use as source.</param>
        /// <returns>Full path.</returns>
        public virtual string GetFullPath(StoragePath storagePath) => GetFullPath(storagePath.LocalPath);

        public virtual StorageRecord RenameRecord(StorageRecord storageRecord, string name, IProgress progress = null)
        {
            var targetLocalPath = storageRecord.LocalPath.ChangeName(name);
            var sourceFullPath = GetFullPath(storageRecord);
            var targetFullPath = GetFullPath(targetLocalPath);
            if (null != progress)
            {
                progress.Total = 1;
            }
            try
            {
                File.Move(sourceFullPath, targetFullPath);
                if (null != progress)
                {
                    progress.Value = 1;
                }
                Logger.LogDebug("Successfully renamed \"{0}\" to \"{1}\".", sourceFullPath, targetFullPath);
                return new StorageRecord(this, targetLocalPath, storageRecord.MediaType);
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Failed to rename \"{0}\" to \"{1}\".", sourceFullPath, targetFullPath);
                throw;
            }
        }

        public virtual async Task<string> GetMediaTypeAsync(FsPath localPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fsPath = GetFullPath(localPath);
            string mediaType;
            if (null != ContentAnalyzer)
            {
                using (var stream = new FileStream(fsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous))
                {
                    var contentInfo = await ContentAnalyzer.Analyze(stream, Path.GetFileName(fsPath), true, cancellationToken);
                    if (null != contentInfo && !string.IsNullOrEmpty(contentInfo.MediaType))
                    {
                        Logger.LogDebug("Successfully detected media type for \"{0}\" as \"{1}\".", fsPath, contentInfo.MediaType);
                        mediaType = contentInfo.MediaType;
                    }
                    else
                    {
                        Logger.LogDebug("Unable to detect media type for \"{0}\".", fsPath);
                        mediaType = "application/octet-stream";
                    }
                }
            }
            else
            {
                Logger.LogDebug("No content type analyzer specified to detect media type for \"{0}\".", fsPath);
                mediaType = "application/octet-stream";
            }
            return mediaType;
        }

        protected abstract IEnumerable<string> GetFileSystemEntries(FsPath localPath);

        public IAsyncEnumerable<IStorageItem> GetContentsAsync() => GetContentsAsync(null);

        public virtual Task<IStorageRecord> CreateRecordAsync(string name, Stream contents, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => CreateRecordAsync(FsPath.Parse(name), contents, progress, cancellationToken);

        public virtual Task<IStorageFolder> CreateFolderAsync(string name, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
            => CreateFolderAsync(FsPath.Parse(name), progress, cancellationToken);
    }
}