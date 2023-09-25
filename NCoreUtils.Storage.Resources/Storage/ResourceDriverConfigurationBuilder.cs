using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NCoreUtils.Storage.Resources;

namespace NCoreUtils.Storage
{
    public class ResourceDriverConfigurationBuilder
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        public IServiceCollection Services { get; }

        public ResourceDriverConfigurationBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public ResourceDriverConfigurationBuilder AddAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly);
            return this;
        }

        public ResourceDriverConfigurationBuilder ConfigureProvider<T>(Assembly assembly, string name, string prefix = "")
        {
            Services.AddSingleton<IStorageProvider<T>>(serviceProvider =>
            {
                var driver = serviceProvider.GetRequiredService<ResourceStorageDriver>();
                var fileProvideer = new EmbeddedFileProvider(assembly);
                return new ConfiguredStorageProvider<T>(new FileProviders.StorageProvider(driver, name, fileProvideer, prefix));
            });
            return this;
        }
    }
}