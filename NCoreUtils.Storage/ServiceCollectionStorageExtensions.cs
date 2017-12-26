using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Storage
{
    public static class ServiceCollectionStorageExtensions
    {
        static StorageProviderCollection GetStorageProviderCollection(this IServiceCollection services)
        {
            var collection = services
                .Where(desc => desc.ServiceType == typeof(StorageProviderCollection) && null != desc.ImplementationInstance)
                .Select(desc => desc.ImplementationInstance)
                .OfType<StorageProviderCollection>()
                .FirstOrDefault();
            if (null == collection)
            {
                collection = new StorageProviderCollection();
                services.AddSingleton(collection);
                // storage provider factory added with collection -- once
                services.AddSingleton<IStorageProvider>(serviceProvider =>
                {
                    var cc = serviceProvider.GetRequiredService<StorageProviderCollection>();
                    if (cc.Count == 1)
                    {
                        return (IStorageProvider)ActivatorUtilities.CreateInstance(serviceProvider, cc[0]);
                    }
                    return new CompositeStorageProvider(cc.Select(ty => (IStorageProvider)ActivatorUtilities.CreateInstance(serviceProvider, ty)).ToImmutableArray());
                });
            }
            return collection;
        }

        public static IServiceCollection AddStorageProvider<TStorageProvider>(this IServiceCollection services)
            where TStorageProvider : IStorageProvider
        {
            services.GetStorageProviderCollection().Add(typeof(TStorageProvider));
            return services;
        }
    }
}