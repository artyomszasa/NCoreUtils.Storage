using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Storage.FileSystem;

namespace NCoreUtils.Storage
{
    public static class ServiceCollectionFileSystemStorageExtensions
    {
        static readonly bool _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static IServiceCollection AddFileSystemStorageProvider(this IServiceCollection services)
            => _isLinux ? services.AddStorageProvider<LinuxStorageProvider>() : throw new NotImplementedException("Windows is not yet supported.");
    }
}