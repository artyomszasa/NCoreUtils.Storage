using System;

namespace NCoreUtils.Storage
{
    public struct StorageActor : IEquatable<StorageActor>
    {
        public static StorageActor Public { get; } = new StorageActor(StorageActorType.Public, null);

        public static StorageActor Authenticated { get; } = new StorageActor(StorageActorType.Authenticated, null);

        public static StorageActor User(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException("User id must be non empty string", nameof(id));
            }
            return new StorageActor(StorageActorType.User, id);
        }

        public static StorageActor Group(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException("Group id must be non empty string", nameof(id));
            }
            return new StorageActor(StorageActorType.Group, id);
        }

        public StorageActorType ActorType { get; }

        public string Id { get; }

        public StorageActor(StorageActorType actorType, string id)
        {
            ActorType = actorType;
            Id = id;
        }

        public bool Equals(StorageActor other)
        {
            switch (ActorType)
            {
                case StorageActorType.Public:
                    return other.ActorType == StorageActorType.Public;
                case StorageActorType.Authenticated:
                    return other.ActorType == StorageActorType.Authenticated;
                case StorageActorType.User:
                    return other.ActorType == StorageActorType.User && Id == other.Id;
                case StorageActorType.Group:
                    return other.ActorType == StorageActorType.Group && Id == other.Id;
                default:
                    return false;
            }
        }

        public override bool Equals(object obj) => obj is StorageActor other && Equals(other);

        public override int GetHashCode()
        {
            switch (ActorType)
            {
                case StorageActorType.Public:
                    return (int)StorageActorType.Public;
                case StorageActorType.Authenticated:
                    return (int)StorageActorType.Authenticated;
                case StorageActorType.User:
                    return (Id.GetHashCode() << 2) | (int)StorageActorType.User;
                case StorageActorType.Group:
                    return (Id.GetHashCode() << 2) | (int)StorageActorType.Group;
                default:
                    return -1;
            }
        }
    }
}