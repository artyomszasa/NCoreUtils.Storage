using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Storage.FileSystem
{
    public class FileSystemProviderConfigurationBuilder
    {
        public IServiceCollection Services { get; }

        public FileSystemProviderConfigurationBuilder(IServiceCollection services)
            => Services = services ?? throw new ArgumentNullException(nameof(services));

        public FileSystemProviderConfigurationBuilder AddProvider<T>(
            string pathPrefix,
            bool createFolder = false)
        {
            Services.AddSingleton<IStorageProvider<T>>(serviceProvider =>
            {
                var driver = serviceProvider.GetRequiredService<StorageDriverProxy>().Driver;
                var fullPathPrefix = Path.GetFullPath(pathPrefix);
                if (driver is UnixStorageDriver unixDriver)
                {
                    var unixPath = unixDriver.Root.Resolve(GenericSubpath.Parse(fullPathPrefix.TrimStart('/')));
                    if (!(unixPath is StorageFolder folder))
                    {
                        if (!createFolder)
                        {
                            throw new InvalidOperationException($"Unable to create provider for path \"{fullPathPrefix}\": path does not exist.");
                        }
                        folder = unixDriver.Root.CreateFolder(unixPath.Subpath, null);
                    }
                    return new ConfiguredStorageProvider<T>(new StorageProvider(
                        driver: unixDriver,
                        pathPrefix: unixDriver.Root.GetFullPath(folder.Subpath) +'/',
                        uriPathPrefix: unixDriver.Root.GetFullUriPath(folder.Subpath) + '/',
                        directorySeparator: '/'
                    ));
                }
                if (driver is WinStorageDriver winDriver)
                {
                    var driveLetter = System.IO.Path.GetPathRoot(fullPathPrefix).Trim(':', '\\');
                    pathPrefix = pathPrefix.Substring(driveLetter.Length + 2);
                    var root = winDriver.GetRoots().FirstOrDefault(root => root.DriveLetter == driveLetter);
                    if (root is null)
                    {
                        throw new InvalidOperationException($"Unable to find root for drive \"{driveLetter}:\\\".");
                    }
                    var winPath = root.Resolve(GenericSubpath.Parse(pathPrefix));
                    if (!(winPath is StorageFolder folder))
                    {
                        if (!createFolder)
                        {
                            throw new InvalidOperationException($"Unable to create provider for path \"{fullPathPrefix}\": path does not exist.");
                        }
                        folder = root.CreateFolder(winPath.Subpath, null);
                    }
                    return new ConfiguredStorageProvider<T>(new StorageProvider(
                        driver: winDriver,
                        pathPrefix: root.GetFullPath(folder.Subpath) + '\\',
                        uriPathPrefix: root.GetFullUriPath(folder.Subpath) + '/',
                        directorySeparator: '\\'
                    ));
                }
                throw new InvalidOperationException("Invalid fily system driver.");
            });
            return this;
        }
    }
}