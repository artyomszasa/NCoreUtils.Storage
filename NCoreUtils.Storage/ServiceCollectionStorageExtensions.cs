using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Storage;

namespace NCoreUtils
{
    public static class ServiceCollectionStorageExtensions
    {
        public static IServiceCollection AddStorage(this IServiceCollection services, Action<StorageConfigurationBuilder> configure)
        {
            var builder = new StorageConfigurationBuilder(services);
            configure(builder);
            switch (builder._drivers.Count)
            {
                case 0:
                    throw new InvalidOperationException("No storage providers has been registered, consider removing services.AddStorage(...).");
                case 1:
                    services.AddSingleton(typeof(IStorageDriver), builder._drivers[0]);
                    break;
                default:
                    services.AddSingleton<CompositeStoreageDriver>(serviceProvider =>
                    {
                        var drivers = builder._drivers
                            .Select(driverType => (IStorageDriver)serviceProvider.GetService(driverType))
                            .ToArray();
                        return new CompositeStoreageDriver(drivers);
                    });
                    break;
            }
            return services;
        }
    }
}