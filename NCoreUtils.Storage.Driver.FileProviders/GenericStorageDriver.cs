using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace NCoreUtils.Storage.FileProviders
{
    public sealed class GenericStorageDriver : StorageDriver
    {
        private readonly ConcurrentDictionary<string, StorageRoot> _roots = new ConcurrentDictionary<string, StorageRoot>();

        private readonly IGenericStorageDriverConfiguration _configuration;

        public GenericStorageDriver(IGenericStorageDriverConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private StorageRoot GetOrCreateStorageRoot(string name, IFileProvider fileProvider)
        {
            if (_roots.TryGetValue(name, out var root))
            {
                return root;
            }
            return _roots.GetOrAdd(name, new StorageRoot(this, name, fileProvider));
        }

        public override IEnumerable<StorageRoot> GetRootsAsync()
            => _configuration.Select(kv => GetOrCreateStorageRoot(kv.Key, kv.Value));
    }
}