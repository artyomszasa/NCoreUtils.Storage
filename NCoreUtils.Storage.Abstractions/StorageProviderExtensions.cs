using System;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils.Storage
{
    public static class StorageProviderExtensions
    {
        public static IEnumerable<IStorageRoot> GetRoots(this IStorageProvider storageProvider) => storageProvider.GetRootsAsync().ToEnumerable();

        public static IStoragePath Resolve(this IStorageProvider storageProvider, Uri uri) => storageProvider.ResolveAsync(uri).GetAwaiter().GetResult();
    }
}