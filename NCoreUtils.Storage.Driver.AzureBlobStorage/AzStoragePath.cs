namespace NCoreUtils.Storage.AzureBlobStorage
{
    public class AzStoragePath : IStoragePath
    {
        private readonly GenericSubpath _subpath;

        IStorageProvider IStoragePath.Provider => Provider;

        public AzStorageProvider Provider { get; }

        public ref readonly GenericSubpath Subpath => ref _subpath;

        public AzStoragePath(AzStorageProvider provider, in GenericSubpath subpath)
        {
            _subpath = subpath;
            Provider = provider;
        }
    }
}