using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.ContentDetection;
using Xunit;

namespace NCoreUtils.Storage.Unit
{
    public class GoogleTests : IDisposable
    {
        readonly IReadOnlyList<string> _buckets;

        readonly ConcurrentDictionary<string, FakeGoogleClient.Entry> _entries;

        readonly IServiceProvider _serviceProvider;

        public GoogleTests()
        {
            var buckets = new List<string> { "dummy" };
            _buckets = buckets;
            _entries = new ConcurrentDictionary<string, FakeGoogleClient.Entry>();
            var client = new FakeGoogleClient(buckets, _entries);
            _serviceProvider = new ServiceCollection()
                .AddSingleton(client)
                .AddLogging()
                .AddGoogleCloudStorageProvider<FakeGoogleProvider>("dummy")
                // .AddTransient<IStorageProvider>(serviceProvider => serviceProvider.GetRequiredService<FakeGoogleProvider>())
                .ConfigureContentDetection(b => b.AddMagic(bb => bb.AddLibmagicRules()))
                .BuildServiceProvider();
        }

        void IDisposable.Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        void Scoped(Action<IServiceProvider> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                action(scope.ServiceProvider);
            }
        }

        async Task ScopedAsync(Func<IServiceProvider, Task> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                await action(scope.ServiceProvider);
            }
        }

        [Fact]
        public void GetRoots() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is GoogleCloudStorage.StorageRoot bucket && "dummy" == bucket.BucketName && "gs://dummy/" == bucket.Uri.ToString());
        });

        [Fact]
        public void CreateRecord() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://dummy/x.png"));
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

        [Fact]
        public void CreateRecordStream() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://dummy/x.png"));
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


        [Fact]
        public void CreateRecordWithoutMediaType() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://dummy/x.png"));
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

        [Fact]
        public void CreateRecordInSubfolder() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://dummy/img/x.png"));
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
            var folder = provider.Resolve(new Uri($"gs://dummy/img"));
            Assert.Equal(Resources.Png.X, data);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);
            Assert.Equal(folder.Uri, record0.GetParent().Uri);

            Assert.Single(((GoogleCloudStorage.StorageFolder)folder).GetContents(), item => item.IsRecord() && item.Uri == record0.Uri);

            record1.Delete();
            var record2 = provider.Resolve(path.Uri);
            Assert.False(record2.IsRecord());
        });

        [Fact]
        public void CreateRecordInSubfolderWithDelete() => Scoped(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IStorageProvider>();
            var path = provider.Resolve(new Uri($"gs://dummy/img/x.png"));
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
            var folder = provider.Resolve(new Uri($"gs://dummy/img"));
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