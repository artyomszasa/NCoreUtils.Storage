using System;

namespace NCoreUtils.Storage
{
    public static class StoragePathExtensions
    {
        public static Uri GetUri(this IStoragePath path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            return path.Provider.GetUri(in path.Subpath);
        }
    }
}