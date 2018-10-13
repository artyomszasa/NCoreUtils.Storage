using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.ContentDetection;
using Xunit;

namespace NCoreUtils.Storage.Unit
{
    public abstract class GoogleTestsBase : IDisposable
    {
        readonly IServiceProvider _serviceProvider;

        readonly string _bucketName;

        public GoogleTestsBase(string bucketName, Func<IServiceCollection, IServiceCollection> setup)
        {
            _bucketName = bucketName ?? "dummy";
            _serviceProvider = setup(new ServiceCollection()
                .AddLogging()
                .ConfigureContentDetection(b => b.AddMagic(bb => bb.AddLibmagicRules())))
                .BuildServiceProvider();
        }

        void IDisposable.Dispose() => (_serviceProvider as IDisposable)?.Dispose();

        protected void Scoped(Action<IServiceProvider> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                action(scope.ServiceProvider);
            }
        }

        protected async Task ScopedAsync(Func<IServiceProvider, Task> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                await action(scope.ServiceProvider);
            }
        }

        public virtual void GetRoots() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is GoogleCloudStorage.StorageRoot bucket && _bucketName == bucket.BucketName && $"gs://{_bucketName}/" == bucket.Uri.ToString());
        });

        public virtual void CreateRecord() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://{_bucketName}/x.png"));
            var record0 = (GoogleCloudStorage.StorageRecord)provider.CreateRecord(path, Resources.Png.X, "image/png");
            var record1 = (GoogleCloudStorage.StorageRecord)provider.Resolve(path.Uri);
            Assert.Equal(record0.Uri, record1.Uri);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                record1.CopyTo(memoryStream);
                memoryStream.Flush();
                data = memoryStream.ToArray();
            }
            Assert.Equal(Resources.Png.X, data);
            Assert.Equal(record0.StorageRoot, record0.GetParent());
            Assert.Equal(path.StorageRoot, record0.GetParent());

            record1.Delete();
            var record2 = provider.Resolve(path.Uri);
            Assert.False(record2.IsRecord());
        });

        public virtual void CreateRecordStream() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://{_bucketName}/x.png"));
            var record0 = (GoogleCloudStorage.StorageRecord)provider.CreateRecord(path, Resources.Png.X, "image/png");
            var record1 = (GoogleCloudStorage.StorageRecord)provider.Resolve(path.Uri);
            Assert.Equal(record0.Uri, record1.Uri);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                using (var source = record1.CreateReadableStream())
                {
                    source.CopyTo(memoryStream);
                }
                memoryStream.Flush();
                data = memoryStream.ToArray();
            }
            Assert.Equal(Resources.Png.X, data);
        });


        public virtual void CreateRecordWithoutMediaType() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://{_bucketName}/x.png"));
            var record0 = (GoogleCloudStorage.StorageRecord)provider.CreateRecord(path, Resources.Png.X);
            var record1 = (GoogleCloudStorage.StorageRecord)provider.Resolve(path.Uri);
            Assert.Equal(record0.Uri, record1.Uri);
            Assert.Equal("image/png", record0.MediaType);
            Assert.Equal("x.png", record0.Name);
            Assert.Equal("x.png", record1.Name);
            Assert.Equal(record0.MediaType, record1.MediaType);
            Assert.Equal(record0.Size, record1.Size);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                record1.CopyTo(memoryStream);
                memoryStream.Flush();
                data = memoryStream.ToArray();
            }
            Assert.Equal(Resources.Png.X, data);
        });

        public virtual void CreateRecordInSubfolder() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://{_bucketName}/img/x.png"));
            var record0 = (GoogleCloudStorage.StorageRecord)provider.CreateRecord(path, Resources.Png.X, "image/png");
            var record1 = (GoogleCloudStorage.StorageRecord)provider.Resolve(path.Uri);
            Assert.Equal(record0.Uri, record1.Uri);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                record1.CopyTo(memoryStream);
                memoryStream.Flush();
                data = memoryStream.ToArray();
            }
            var folder = provider.Resolve(new Uri($"gs://{_bucketName}/img"));
            Assert.Equal(Resources.Png.X, data);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);

            Assert.Single(((GoogleCloudStorage.StorageFolder)folder).GetContents(), item => item.IsRecord() && item.Uri == record0.Uri);

            record1.Delete();
            var record2 = provider.Resolve(path.Uri);
            Assert.False(record2.IsRecord());
        });

        public virtual void CreateRecordInSubfolderWithDelete() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://{_bucketName}/img/x.png"));
            var record0 = (GoogleCloudStorage.StorageRecord)provider.CreateRecord(path, Resources.Png.X, "image/png");
            var record1 = (GoogleCloudStorage.StorageRecord)provider.Resolve(path.Uri);
            Assert.Equal(record0.Uri, record1.Uri);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                record1.CopyTo(memoryStream);
                memoryStream.Flush();
                data = memoryStream.ToArray();
            }
            var folder = provider.Resolve(new Uri($"gs://{_bucketName}/img"));
            Assert.Equal(Resources.Png.X, data);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);

            Assert.Single(((GoogleCloudStorage.StorageFolder)folder).GetContents(), item => item.IsRecord() && item.Uri == record0.Uri);

            ((GoogleCloudStorage.StorageFolder)folder).Delete();
            var record2 = provider.Resolve(path.Uri);
            Assert.False(record2.IsRecord());
        });
    }
}