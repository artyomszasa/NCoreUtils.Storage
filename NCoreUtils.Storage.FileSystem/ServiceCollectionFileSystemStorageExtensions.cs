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
            => _isLinux ? services.AddStorageProvider<LinuxStorageProvider>() : services.AddStorageProvider<WindowsStorageProvider>();

        public static IServiceCollection AddFileSystemStorageProvider(this IServiceCollection services, string rootPath)
        {
            services.AddSingleton(new FileSystemStorageOptions { RootPath = rootPath });
            return _isLinux ? services.AddStorageProvider<LinuxStorageProvider>() : services.AddStorageProvider<WindowsStorageProvider>();
        }
    }
}