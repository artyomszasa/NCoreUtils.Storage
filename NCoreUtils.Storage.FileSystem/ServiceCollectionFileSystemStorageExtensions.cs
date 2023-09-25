using System;
using NCoreUtils.Storage.FileSystem;

namespace NCoreUtils.Storage
{
    public static class ServiceCollectionFileSystemStorageExtensions
    {
        public static StorageConfigurationBuilder AddFileSystemDriver(
            this StorageConfigurationBuilder builder,
            Action<FileSystemProviderConfigurationBuilder>? configure)
        {
            if (!(configure is null))
            {
                var b = new FileSystemProviderConfigurationBuilder(builder.Services);
                configure(b);
            }
            return builder.AddDriver<FileSystem.StorageDriverProxy>();
        }
    }
}