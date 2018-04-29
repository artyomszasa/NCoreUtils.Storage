using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using Xunit;

namespace NCoreUtils.Storage.FileSystem
{
    public class BasicTests : IDisposable
    {
        readonly IServiceProvider _serviceProvider;

        public BasicTests()
        {
            _serviceProvider = new ServiceCollection()
                .AddLogging(b => b.ClearProviders().AddDebug().SetMinimumLevel(LogLevel.Trace))
                .ConfigureContentDetection(b => b.AddMagic(bb => bb.AddLibmagicRules()))
                .AddSingleton<IFeatureCollection<IStorageProvider>>(new FeatureCollectionBuilder().Build<IStorageProvider>())
                .AddFileSystemStorageProvider()
                .BuildServiceProvider();
        }

        async Task ScopedAsync(Func<IServiceProvider, Task> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                await action(scope.ServiceProvider);
            }
        }

        void IDisposable.Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        string CreateUniqueFolder(string path, string prefix)
        {
            while (true)
            {

                var candidate = $"{prefix}-{DateTimeOffset.Now.UtcTicks}";
                var folderPath = Path.Combine(path, candidate);
                if (Directory.Exists(folderPath) || File.Exists(folderPath))
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Directory.CreateDirectory(folderPath);
                    return folderPath;
                }
            }
        }

        [Fact]
        public Task CreateRecord()
            => ScopedAsync(async serviceProvider =>
            {
                var tmpPath = Path.GetTempPath();
                var storage = serviceProvider.GetRequiredService<IStorageProvider>();
                var item = await storage.ResolveAsync(new Uri($"file://{tmpPath}"));
                Assert.NotNull(item);
                if (item is IStorageFolder folder)
                {
                    var testFolderPath = CreateUniqueFolder(tmpPath, "storage-unit");
                    try
                    {
                        var testFolderItem = await storage.ResolveAsync(new Uri($"file://{testFolderPath}"));
                        Assert.NotNull(testFolderItem);
                        if (testFolderItem is IStorageFolder testFolder)
                        {
                            var data = Resources.Png.X;
                            var record = await testFolder.CreateRecordAsync("x.png", data);
                            Assert.NotNull(record);
                            // CHECK FS
                            var fileInfo = new FileInfo(Path.Combine(testFolderPath, "x.png"));
                            Assert.True(fileInfo.Exists);
                            Assert.Equal(data.Length, fileInfo.Length);
                            Assert.Equal((IEnumerable<byte>)data, File.ReadAllBytes(fileInfo.FullName));
                            // CHECK STORAGE
                            var subitems = await testFolder.GetContentsAsync().ToList();
                            Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems.Count);
                            var checkRecord = subitems.OfType<IStorageRecord>().FirstOrDefault(rec => rec.Name == "x.png");
                            Assert.NotNull(checkRecord);
                            Assert.Equal(data.Length, checkRecord.Size);
                            Assert.Equal((IEnumerable<byte>)data, await checkRecord.ReadAllBytesAsync());
                            return; // success
                        }
                        throw new InvalidCastException($"Non-folder item returned for \"{testFolderPath}\".");
                    }
                    finally
                    {
                        if (Directory.Exists(testFolderPath))
                        {
                            Directory.Delete(testFolderPath, true);
                        }
                    }
                }
                throw new InvalidCastException($"Non-folder item retrned for \"{tmpPath}\".");
            });
    }
}