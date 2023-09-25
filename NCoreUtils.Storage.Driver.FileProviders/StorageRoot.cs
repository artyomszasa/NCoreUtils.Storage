using Microsoft.Extensions.FileProviders;

namespace NCoreUtils.Storage.FileProviders
{
    public class StorageRoot : StorageProvider, IStorageRoot
    {
        public StorageRoot(StorageDriver driver, string assemblyName, IFileProvider fileProvider)
            : base(driver, assemblyName, fileProvider, string.Empty)
        { }
    }
}