using System;

namespace NCoreUtils.Storage
{
    public readonly struct StorageActor : IEquatable<StorageActor>
    {
        public static StorageActor Public { get; } = new StorageActor(StorageActorType.Public, null);

        public static StorageActor Authenticated { get; } = new StorageActor(StorageActorType.Authenticated, null);

        public static StorageActor User(string id)
        {
            if (null == id)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("User id must be non empty string", nameof(id));
            }
            return new StorageActor(StorageActorType.User, id);
        }

        public static StorageActor Group(string id)
        {
            if (null == id)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Group id must be non empty string", nameof(id));
            }
            return new StorageActor(StorageActorType.Group, id);
        }

        public StorageActorType ActorType { get; }

        public string? Id { get; }

        public StorageActor(StorageActorType actorType, string? id)
        {
            if (actorType != StorageActorType.Public && actorType != StorageActorType.Authenticated && id is null)
            {
                throw new ArgumentNullException(nameof(id), "Identifier must be a non-null value for users and groupd.");
            }
            ActorType = actorType;
            Id = id;
        }

        public void Deconstruct(out StorageActorType actorType, out string? id)
        {
            actorType = ActorType;
            id = Id;
        }

        public bool Equals(StorageActor other)
        {
            return ActorType switch
            {
                StorageActorType.Public => other.ActorType == StorageActorType.Public,
                StorageActorType.Authenticated => other.ActorType == StorageActorType.Authenticated,
                StorageActorType.User => other.ActorType == StorageActorType.User && Id == other.Id,
                StorageActorType.Group => other.ActorType == StorageActorType.Group && Id == other.Id,
                _ => false,
            };
        }

        public override bool Equals(object? obj) => obj is StorageActor other && Equals(other);

        public override int GetHashCode()
        {
            switch (ActorType)
            {
                case StorageActorType.Public:
                    return (int)StorageActorType.Public;
                case StorageActorType.Authenticated:
                    return (int)StorageActorType.Authenticated;
                case StorageActorType.User:
                    return (Id!.GetHashCode() << 2) | (int)StorageActorType.User;
                case StorageActorType.Group:
                    return (Id!.GetHashCode() << 2) | (int)StorageActorType.Group;
                default:
                    return -1;
            }
        }
    }
}