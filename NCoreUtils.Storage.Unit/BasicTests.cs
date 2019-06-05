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
using NCoreUtils.Storage.Unit;
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

        void WithTestFolder(Action<IServiceProvider, IStorageFolder, string> action)
            => Scoped(serviceProvider =>
            {
                var tmpPath = Path.GetTempPath();
                var storage = serviceProvider.GetRequiredService<IStorageProvider>();
                var item = storage.Resolve(new Uri($"file://{tmpPath}"));
                Assert.NotNull(item);
                if (item is IStorageFolder folder)
                {
                    var testFolderPath = CreateUniqueFolder(tmpPath, "storage-unit");
                    try
                    {
                        var testFolderItem = storage.Resolve(new Uri($"file://{testFolderPath}"));
                        Assert.NotNull(testFolderItem);
                        if (testFolderItem is IStorageFolder testFolder)
                        {
                            action(serviceProvider, testFolder, testFolderPath);
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

        Task WithTestFolderAsync(Func<IServiceProvider, IStorageFolder, string, Task> action)
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
                            await action(serviceProvider, testFolder, testFolderPath);
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


        [Fact]
        public Task CreateAndDeleteRecord()
            => WithTestFolderAsync(async (serviceProvider, testFolder, testFolderPath) =>
            {
                var data = Resources.Png.X;
                var fileFullPath = Path.Combine(testFolderPath, "x.png");
                // CREATE RECORD
                var record = await testFolder.CreateRecordAsync("x.png", data);
                Assert.NotNull(record);
                // CHECK FS
                var fileInfo = new FileInfo(fileFullPath);
                Assert.True(fileInfo.Exists);
                Assert.Equal(data.Length, fileInfo.Length);
                Assert.Equal((IEnumerable<byte>)data, File.ReadAllBytes(fileInfo.FullName));
                // CHECK STORAGE RECORD
                var subitems = await Internal.AsyncEnumerable.ToList(testFolder.GetContentsAsync(), CancellationToken.None);
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems.Count);
                var checkRecord = subitems.OfType<IStorageRecord>().FirstOrDefault(rec => rec.Name == "x.png");
                Assert.NotNull(checkRecord);
                Assert.Equal(data.Length, checkRecord.Size);
                Assert.Equal((IEnumerable<byte>)data, await checkRecord.ReadAllBytesAsync());
                /// DELETE RECORD
                var deleteProgress = new DummyProgress();
                await record.DeleteAsync(deleteProgress);
                Assert.Equal(deleteProgress.Total, deleteProgress.Value);
                await Assert.ThrowsAsync<FileNotFoundException>(() => record.DeleteAsync());
                // CHECK FS
                Assert.False(File.Exists(fileFullPath));
                var subitems1 = await Internal.AsyncEnumerable.ToList(testFolder.GetContentsAsync(), CancellationToken.None);
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems1.Count);
                // CHECK STORAGE
                var noRecord = await record.GetStorageProvider().ResolveAsync(record.Uri);
                Assert.NotNull(noRecord);
                Assert.Null(noRecord as IStorageItem);
                Assert.NotNull(noRecord as IStoragePath);
            });

        [Fact]
        public Task CreateAndDeleteFolder()
            => WithTestFolderAsync(async (serviceProvider, testFolder, testFolderPath) =>
            {
                var folderFullPath = Path.Combine(testFolderPath, "folder");
                // CREATE RECORD
                var folder = await testFolder.CreateFolderAsync("folder");
                Assert.NotNull(folder);
                Assert.Equal("folder", folder.Name);
                // CHECK FS
                var dirInfo = new DirectoryInfo(folderFullPath);
                Assert.True(dirInfo.Exists);
                // CHECK STORAGE FOLDER
                var subitems = await Internal.AsyncEnumerable.ToList(testFolder.GetContentsAsync(), CancellationToken.None);
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems.Count);
                var checkFolder = subitems.OfType<IStorageFolder>().FirstOrDefault(rec => rec.Name == "folder");
                Assert.NotNull(checkFolder);
                /// DELETE FOLDER
                var deleteProgress = new DummyProgress();
                await folder.DeleteAsync(deleteProgress);
                Assert.Equal(deleteProgress.Total, deleteProgress.Value);
                await Assert.ThrowsAsync<DirectoryNotFoundException>(() => folder.DeleteAsync());
                // CHECK FS
                Assert.False(Directory.Exists(folderFullPath));
                var subitems1 = await Internal.AsyncEnumerable.ToList(testFolder.GetContentsAsync(), CancellationToken.None);
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems1.Count);
                // CHECK STORAGE
                var noFolder = await folder.GetStorageProvider().ResolveAsync(folder.Uri);
                Assert.NotNull(noFolder);
                Assert.Null(noFolder as IStorageItem);
                Assert.NotNull(noFolder as IStoragePath);
            });

        [Fact]
        public void CreateAndDeleteRecordSync()
            => WithTestFolder((serviceProvider, testFolder, testFolderPath) =>
            {
                var data = Resources.Png.X;
                var fileFullPath = Path.Combine(testFolderPath, "x.png");
                // CREATE RECORD
                var record = testFolder.CreateRecord("x.png", data);
                Assert.NotNull(record);
                // CHECK FS
                var fileInfo = new FileInfo(fileFullPath);
                Assert.True(fileInfo.Exists);
                Assert.Equal(data.Length, fileInfo.Length);
                Assert.Equal((IEnumerable<byte>)data, File.ReadAllBytes(fileInfo.FullName));
                // CHECK STORAGE RECORD
                var subitems = testFolder.GetContents().ToList();
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems.Count);
                var checkRecord = subitems.OfType<IStorageRecord>().FirstOrDefault(rec => rec.Name == "x.png");
                Assert.NotNull(checkRecord);
                Assert.Equal(data.Length, checkRecord.Size);
                Assert.Equal((IEnumerable<byte>)data, checkRecord.ReadAllBytes());
                /// DELETE RECORD
                var deleteProgress = new DummyProgress();
                record.Delete(deleteProgress);
                Assert.Equal(deleteProgress.Total, deleteProgress.Value);
                Assert.Throws<FileNotFoundException>(() => record.Delete());
                // CHECK FS
                Assert.False(File.Exists(fileFullPath));
                var subitems1 = testFolder.GetContents().ToList();
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems1.Count);
                // CHECK STORAGE
                var noRecord = record.GetStorageProvider().Resolve(record.Uri);
                Assert.NotNull(noRecord);
                Assert.Null(noRecord as IStorageItem);
                Assert.NotNull(noRecord as IStoragePath);
            });

        [Fact]
        public void CreateAndDeleteFolderSync()
            => WithTestFolder((serviceProvider, testFolder, testFolderPath) =>
            {
                var folderFullPath = Path.Combine(testFolderPath, "folder");
                // CREATE RECORD
                var folder = testFolder.CreateFolder("folder");
                Assert.NotNull(folder);
                Assert.Equal("folder", folder.Name);
                // CHECK FS
                var dirInfo = new DirectoryInfo(folderFullPath);
                Assert.True(dirInfo.Exists);
                // CHECK STORAGE FOLDER
                var subitems = testFolder.GetContents().ToList();
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems.Count);
                var checkFolder = subitems.OfType<IStorageFolder>().FirstOrDefault(rec => rec.Name == "folder");
                Assert.NotNull(checkFolder);
                /// DELETE FOLDER
                var deleteProgress = new DummyProgress();
                folder.Delete(deleteProgress);
                Assert.Equal(deleteProgress.Total, deleteProgress.Value);
                Assert.Throws<DirectoryNotFoundException>(() => folder.Delete());
                // CHECK FS
                Assert.False(Directory.Exists(folderFullPath));
                var subitems1 = testFolder.GetContents().ToList();
                Assert.Equal(Directory.GetFileSystemEntries(testFolderPath).Length, subitems1.Count);
                // CHECK STORAGE
                var noFolder = folder.GetStorageProvider().Resolve(folder.Uri);
                Assert.NotNull(noFolder);
                Assert.Null(noFolder as IStorageItem);
                Assert.NotNull(noFolder as IStoragePath);
            });

    }
}