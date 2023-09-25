using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NCoreUtils.Storage;
using NCoreUtils.Storage.Resources;

namespace NCoreUtils
{
    public static class StorageConfigurationBuilderResourcesExtensions
    {
        public static StorageConfigurationBuilder AddEmbeddedResources(this StorageConfigurationBuilder builder, Action<ResourceDriverConfigurationBuilder> configure)
        {
            var b = new ResourceDriverConfigurationBuilder(builder.Services);
            configure(b);
            builder.Services
                .AddOptions<ResourceStorageDriverConfiguration>()
                    .Configure(o => o.AddRange(b.Assemblies))
                    .Services
                .AddTransient<IResourceStorageDriverConfiguration>(serviceProvider => serviceProvider.GetRequiredService<IOptionsMonitor<ResourceStorageDriverConfiguration>>().CurrentValue);
            return builder.AddDriver<ResourceStorageDriver>();
        }
    }
}