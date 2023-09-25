using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using NCoreUtils.Storage.FileProviders;

namespace NCoreUtils.Storage.Resources
{
    public class ResourceStorageDriver : StorageDriver
    {
        private readonly ConcurrentDictionary<string, StorageRoot> _roots = new ConcurrentDictionary<string, StorageRoot>();

        private readonly IReadOnlyDictionary<Assembly, EmbeddedFileProvider> _sources;

        public override string UriScheme => "resx";

        public ResourceStorageDriver(IResourceStorageDriverConfiguration assemblies)
        {
            _sources = assemblies.ToDictionary(e => e, e => new EmbeddedFileProvider(e));
        }

        private StorageRoot GetOrCreateStorageRoot(Assembly assembly, EmbeddedFileProvider fileProvider)
        {
            var name = assembly.GetName().Name;
            if (_roots.TryGetValue(name, out var root))
            {
                return root;
            }
            return _roots.GetOrAdd(name, new StorageRoot(this, name, fileProvider));
        }

        public override IEnumerable<StorageRoot> GetRootsAsync()
            => _sources.Select(kv => GetOrCreateStorageRoot(kv.Key, kv.Value));
    }
}