using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Storage
{

    public class StorageConfigurationBuilder
    {
        internal readonly List<Type> _drivers = new List<Type>();

        public IServiceCollection Services { get; }

        public StorageConfigurationBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public StorageConfigurationBuilder AddDriver(Type driverType)
        {
            if (!typeof(IStorageDriver).IsAssignableFrom(driverType))
            {
                throw new InvalidOperationException($"{driverType} cannot be used as storage driver.");
            }
            Services.AddSingleton(driverType);
            _drivers.Add(driverType);
            return this;
        }

        public StorageConfigurationBuilder AddDriver<TDriver>()
            where TDriver : IStorageDriver
            => AddDriver(typeof(TDriver));
    }
}