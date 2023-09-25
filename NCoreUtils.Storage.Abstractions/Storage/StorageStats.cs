using System;

namespace NCoreUtils.Storage
{
    public class StorageStats
    {
        public static StorageStats DoesNotExist { get; } = new StorageStats(false, default, default, default, default, default);

        public bool Exists { get; }

        public long? Size { get; }

        public string? MediaType { get; }

        public DateTimeOffset? Created { get; }

        public DateTimeOffset? Updated { get; }

        public IStorageSecurity? Acl { get; }

        public StorageStats(bool exists, long? size, string? mediaType, DateTimeOffset? created, DateTimeOffset? updated, IStorageSecurity? acl)
        {
            Exists = exists;
            Size = size;
            MediaType = mediaType;
            Created = created;
            Updated = updated;
            Acl = acl;
        }
    }
}