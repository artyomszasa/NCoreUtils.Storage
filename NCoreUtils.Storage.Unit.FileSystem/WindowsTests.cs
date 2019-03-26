using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NCoreUtils.Features;
using Xunit;

namespace NCoreUtils.Storage.FileSystem
{
    public class WindowsTests
    {
        sealed class DummyLogger : ILogger<WindowsStorageProvider>
        {
            sealed class DummyDisposable : IDisposable
            {
                public void Dispose() { }
            }

            public IDisposable BeginScope<TState>(TState state) => new DummyDisposable();

            public bool IsEnabled(LogLevel logLevel) => false;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }

        [Fact]
        public void GetRoots()
        {
            var features = new FeatureCollectionBuilder().Build<IStorageProvider>();
            var provider = new WindowsStorageProvider(features, new DummyLogger(), null, null);
            Assert.Equal(features.Keys, provider.Features.Keys);
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is WindowsStorageRoot windowsRoot && "C:\\" == windowsRoot.RootPath && "file:///C:/" == windowsRoot.Uri.ToString());
        }

        [Fact]
        public void GetSpecialRoots()
        {
            var provider = new WindowsStorageProvider(new FeatureCollectionBuilder().Build<IStorageProvider>(), new DummyLogger(), null, new FileSystemStorageOptions { RootPath = "C:\\tmp\\" });
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is WindowsStorageRoot windowsRoot && "C:\\tmp\\" == windowsRoot.RootPath && "file:///C:/tmp/" == windowsRoot.Uri.ToString());
        }

        [Fact]
        public void GetContainer()
        {
            var provider = new WindowsStorageProvider(new FeatureCollectionBuilder().Build<IStorageProvider>(), new DummyLogger(), null, new FileSystemStorageOptions { RootPath = "C:\\tmp\\" });
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is WindowsStorageRoot windowsRoot && "C:\\tmp\\" == windowsRoot.RootPath && "file:///C:/tmp/" == windowsRoot.Uri.ToString());
            var ts = DateTimeOffset.Now.UtcTicks.ToString();
            try
            {
                var level1 = roots[0].CreateFolder(ts);
                var level2 = level1.CreateFolder("xxx");
                Assert.Equal(level1.Uri, level2.GetContainer().Uri);
                Assert.Equal(roots[0].Uri, level1.GetContainer().Uri);
            }
            finally
            {
                System.IO.Directory.Delete($"C:\\tmp\\{ts}", true);
            }
        }
        [Fact]
        public void GetSecurities()
        {
            var provider = new WindowsStorageProvider(new FeatureCollectionBuilder().Build<IStorageProvider>(), new DummyLogger(), null, new FileSystemStorageOptions { RootPath = "C:\\tmp\\" });
            var roots = provider.GetRoots().ToArray();
            Assert.Single(roots, root => root is WindowsStorageRoot windowsRoot && "C:\\tmp\\" == windowsRoot.RootPath && "file:///C:/tmp/" == windowsRoot.Uri.ToString());

            var root0 = roots.FirstOrDefault(root => root is WindowsStorageRoot windowsRoot && "C:\\tmp\\" == windowsRoot.RootPath && "file:///C:/tmp/" == windowsRoot.Uri.ToString());
            try
            {
                var record = root0.CreateRecord("teszt.txt", new byte[0]);
                
                Console.WriteLine();
            }
            finally
            {
                System.IO.File.Delete("C:\\tmp\\teszt.txt");

            }


        }
    }
}
