using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage
{
    public static class StorageProviderExtensions
    {
        public static IEnumerable<IStorageRoot> GetRoots(this IStorageProvider storageProvider) => storageProvider.GetRootsAsync().ToEnumerable();

        public static IStoragePath Resolve(this IStorageProvider storageProvider, Uri uri) => storageProvider.ResolveAsync(uri).GetAwaiter().GetResult();

        public static async Task<IStorageFolder> CreateFolderAsync(
            this IStorageProvider storageProvider,
            IStoragePath path,
            bool recursive = false,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (storageProvider == null)
            {
                throw new ArgumentNullException(nameof(storageProvider));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (storageProvider.Features.TryGetFeature(out ICreateByPathFeature implmentation))
            {
                return await implmentation.CreateFolderAsync(storageProvider, path, recursive, progress, cancellationToken);
            }
            // Default implementation.
            var p = await path.GetParentAsync(cancellationToken);
            switch (p)
            {
                case null:
                    throw new InvalidOperationException($"Unable to resolve parent of {path.Uri}.");
                case IStorageFolder folder:
                    return await folder.CreateFolderAsync(path.Name, progress, cancellationToken);
                case IStorageRecord _:
                    throw new InvalidOperationException($"Parent of {path.Uri} is a storage record.");
                case IStoragePath parentPath:
                    if (recursive)
                    {
                        var folder = await storageProvider.CreateFolderAsync(parentPath, true, progress, cancellationToken);
                        return await folder.CreateFolderAsync(path.Name, progress, cancellationToken);
                    }
                    throw new InvalidOperationException($"Parent of {path.Uri} does not exist.");
                default:
                    throw new NotImplementedException($"No implementation provided that handles {p.GetType()}");
            }
        }

        public static async Task<IStorageRecord> CreateRecordAsync(
            this IStorageProvider storageProvider,
            IStoragePath path,
            Stream contents,
            string contentType = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (storageProvider == null)
            {
                throw new ArgumentNullException(nameof(storageProvider));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (storageProvider.Features.TryGetFeature(out ICreateByPathFeature implmentation))
            {
                return await implmentation.CreateRecordAsync(storageProvider, path, contents, contentType, progress, cancellationToken);
            }
            // Default implementation.
            var p = await path.GetParentAsync(cancellationToken);
            switch (p)
            {
                case null:
                    throw new InvalidOperationException($"Unable to resolve parent of {path.Uri}.");
                case IStorageFolder folder:
                    return await folder.CreateRecordAsync(path.Name, contents, contentType, progress, cancellationToken);
                case IStorageRecord _:
                    throw new InvalidOperationException($"Parent of {path.Uri} is a storage record.");
                case IStoragePath parentPath:
                    throw new InvalidOperationException($"Parent of {path.Uri} does not exist.");
                default:
                    throw new NotImplementedException($"No implementation provided that handles {p.GetType()}");
            }
        }

        public static async Task<IStorageRecord> CreateRecordAsync(
            this IStorageProvider storageProvider,
            IStoragePath path,
            byte[] contents,
            string contentType = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var buffer = new MemoryStream(contents, 0, contents.Length, false, true))
            {
                return await storageProvider.CreateRecordAsync(path, buffer, contentType, progress, cancellationToken);
            }
        }

        public static IStorageFolder CreateFolder(
            this IStorageProvider storageProvider,
            IStoragePath path,
            bool recursive = false,
            IProgress progress = null)
        {
            return storageProvider.CreateFolderAsync(path, recursive, progress).GetAwaiter().GetResult();
        }

        public static IStorageRecord CreateRecord(
            this IStorageProvider storageProvider,
            IStoragePath path,
            Stream contents,
            string contentType = null,
            IProgress progress = null)
        {
            return storageProvider.CreateRecordAsync(path, contents, contentType, progress).GetAwaiter().GetResult();
        }

        public static IStorageRecord CreateRecord(
            this IStorageProvider storageProvider,
            IStoragePath path,
            byte[] contents,
            string contentType = null,
            IProgress progress = null)
        {
            return storageProvider.CreateRecordAsync(path, contents, contentType, progress).GetAwaiter().GetResult();
        }
    }
}