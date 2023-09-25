namespace NCoreUtils.Storage.AzureBlobStorage
{
    public class AzStorageRoot : AzStorageProvider, IStorageRoot
    {
        public AzStorageRoot(AzStorageDriver driver, string containerName)
            : base(driver, containerName, default)
        { }
    }
}