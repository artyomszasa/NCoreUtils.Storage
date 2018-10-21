using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NCoreUtils.Storage
{
    public sealed class StorageSecurity : IStorageSecurity, IEquatable<IStorageSecurity>
    {
        public static StorageSecurity Empty => new StorageSecurity(ImmutableDictionary<StorageActor, StoragePermissions>.Empty);

        public static StorageSecurity FullAccess => new StorageSecurity(new Dictionary<StorageActor, StoragePermissions>
        {
            { StorageActor.Public, StoragePermissions.Full }
        }.ToImmutableDictionary());

        readonly ImmutableDictionary<StorageActor, StoragePermissions> _permissions;

        public StorageSecurity(ImmutableDictionary<StorageActor, StoragePermissions> permissions)
            => _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));

        IEnumerator IEnumerable.GetEnumerator() => _permissions.GetEnumerator();

        IEnumerator<KeyValuePair<StorageActor, StoragePermissions>> IEnumerable<KeyValuePair<StorageActor, StoragePermissions>>.GetEnumerator() => _permissions.GetEnumerator();

        public bool Equals(IStorageSecurity other)
        {
            if (null == other)
            {
                return false;
            }
            IReadOnlyDictionary<StorageActor, StoragePermissions> left;
            IReadOnlyDictionary<StorageActor, StoragePermissions> right;
            if (other is StorageSecurity that)
            {
                left = _permissions;
                right = that._permissions;
            }
            else
            {
                left = _permissions;
                right = other.ToImmutableDictionary();
            }
            foreach (var kv in left)
            {
                if (!right.TryGetValue(kv.Key, out var p) || p != kv.Value)
                {
                    return false;
                }
            }
            foreach (var kv in right)
            {
                if (!left.TryGetValue(kv.Key, out var p) || p != kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as IStorageSecurity);

        public override int GetHashCode()
        {
            var hash = 13;
            foreach (var kv in _permissions)
            {
                hash = hash * 17 + kv.GetHashCode();
            }
            return hash;
        }

        public StoragePermissions GetPermissions(StorageActor actor) => _permissions.TryGetValue(actor, out var value) ? value : StoragePermissions.None;

        public IStorageSecurity UpdatePermissions(StorageActor actor, StoragePermissions permissions)
        {
            if (permissions == StoragePermissions.None)
            {
                return _permissions.ContainsKey(actor) ? new StorageSecurity(_permissions.Remove(actor)) : this;
            }
            return new StorageSecurity(_permissions.SetItem(actor, permissions));
        }
    }
}