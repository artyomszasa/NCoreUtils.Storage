namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class GCSStoragePath : IStoragePath
    {
        private readonly GenericSubpath _subpath;

        IStorageProvider IStoragePath.Provider => Provider;

        public GCSStorageProvider Provider { get; }

        public ref readonly GenericSubpath Subpath => ref _subpath;

        public GCSStoragePath(GCSStorageProvider provider, in GenericSubpath subpath)
        {
            _subpath = subpath;
            Provider = provider;
        }
    }
}