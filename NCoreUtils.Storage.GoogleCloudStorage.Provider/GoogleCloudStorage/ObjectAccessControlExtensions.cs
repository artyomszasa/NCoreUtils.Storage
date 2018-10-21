using System;
using Google.Apis.Storage.v1.Data;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    static class ObjectAccessControlExtensions
    {
        public static StoragePermissions GetStoragePermissions(this ObjectAccessControl ac)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals("READER", ac.Role))
            {
                return StoragePermissions.Read;
            }
            if (StringComparer.OrdinalIgnoreCase.Equals("OWNER", ac.Role))
            {
                return StoragePermissions.Full;
            }
            return StoragePermissions.None;
        }

        public static bool TryGetObjectAccessControl(this StoragePermissions permissions, StorageActor actor, out ObjectAccessControl ac)
        {
            if (permissions == StoragePermissions.None)
            {
                ac = null;
                return false;
            }
            ac = new ObjectAccessControl();
            ac.Role = permissions == StoragePermissions.Read ? "READER" : "OWNER";
            switch (actor.ActorType)
            {
                case StorageActorType.Authenticated:
                    ac.Entity = "allAuthenticatedUsers";
                    break;
                case StorageActorType.Public:
                    ac.Entity = "allUsers";
                    break;
                case StorageActorType.User:
                    ac.Entity = $"user-{actor.Id}";
                    ac.EntityId = actor.Id;
                    break;
                case StorageActorType.Group:
                    ac.Entity = $"group-{actor.Id}";
                    ac.EntityId = actor.Id;
                    break;
                default:
                    ac = null;
                    return false;
            }
            return true;
        }
    }
}