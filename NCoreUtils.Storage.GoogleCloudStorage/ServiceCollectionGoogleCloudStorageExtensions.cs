using System;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Storage
{
    public static class ServiceCollectionGoogleCloudStorageExtensions
    {
        public static IServiceCollection AddGoogleCloudStorageProvider(this IServiceCollection services)
            => services.AddStorageProvider<GoogleCloudStorage.StorageProvider>();

        public static IServiceCollection ConfigureGoogleCloudStorageProvider(this IServiceCollection services, string projectId, Action<GoogleCloudStorageOptionsBuilder> configure = null)
        {
            var builder = new GoogleCloudStorageOptionsBuilder(projectId);
            configure?.Invoke(builder);
            return services
                .AddSingleton(builder)
                .AddSingleton<GoogleCloudStorageOptions>();
        }

        public static IServiceCollection ConfigureGoogleCloudStorageProvider<TProvider>(this IServiceCollection services, string projectId, Action<GoogleCloudStorageOptionsBuilder<TProvider>> configure = null)
            where TProvider : GoogleCloudStorage.StorageProvider
        {
            var builder = new GoogleCloudStorageOptionsBuilder<TProvider>(projectId);
            configure?.Invoke(builder);
            return services
                .AddSingleton(builder)
                .AddSingleton<GoogleCloudStorageOptions<TProvider>>();
        }

        public static IServiceCollection AddGoogleCloudStorageProvider(this IServiceCollection services, string projectId, Action<GoogleCloudStorageOptionsBuilder> configure = null)
            => services
                .AddStorageProvider<GoogleCloudStorage.StorageProvider>()
                .ConfigureGoogleCloudStorageProvider(projectId, configure);

        public static IServiceCollection AddGoogleCloudStorageProvider<TProvider>(this IServiceCollection services, string projectId, Action<GoogleCloudStorageOptionsBuilder<TProvider>> configure = null)
            where TProvider : GoogleCloudStorage.StorageProvider
            => services
                .AddStorageProvider<TProvider>()
                .ConfigureGoogleCloudStorageProvider<TProvider>(projectId, configure);
    }
}