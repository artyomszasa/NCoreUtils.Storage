using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NCoreUtils.Features;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    class CompositeFeatureCollection : IFeatureCollection
    {
        readonly IFeatureCollection _inner;

        readonly IFeatureCollection _features;

        public object this[Type key]
        {
            get
            {
                if (TryGetValue(key, out var result))
                {
                    return result;
                }
                throw new KeyNotFoundException($"Collection contains no feature for type {key?.FullName}.");
            }
        }

        public IEnumerable<Type> Keys => GetEntries().Select(e => e.Key);

        public IEnumerable<object> Values => GetEntries().Select(e => e.Value);

        public int Count => GetEntries().Count();

        public CompositeFeatureCollection(IFeatureCollection inner, IFeatureCollection features)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _features = features;
        }

        IEnumerable<KeyValuePair<Type, object>> GetEntries()
        {
            var keys = new HashSet<Type>();
            foreach (var kv in _features)
            {
                keys.Add(kv.Key);
                yield return kv;
            }
            foreach (var kv in _inner)
            {
                if (!keys.Contains(kv.Key))
                {
                    yield return kv;
                }
            }
        }

        public bool ContainsKey(Type key) => _features.ContainsKey(key) || _inner.ContainsKey(key);

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => GetEntries().GetEnumerator();

        public bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : class
        {
            return _features.TryGetFeature(out feature) || _inner.TryGetFeature(out feature);
        }

        public bool TryGetValue(Type key, out object value)
        {
            if (_features.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }
            return _inner.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}