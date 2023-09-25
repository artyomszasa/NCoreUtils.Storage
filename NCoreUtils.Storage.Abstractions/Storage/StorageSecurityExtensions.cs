namespace NCoreUtils.Storage
{
    public static class StorageSecurityExtensions
    {
        public static StoragePermissions GetPublicPermissions(this IStorageSecurity security)
            => security.GetPermissions(StorageActor.Public);

        public static StoragePermissions GetAuthenticatedPermissions(this IStorageSecurity security)
            => security.GetPermissions(StorageActor.Authenticated);

        public static StoragePermissions GetUserPermissions(this IStorageSecurity security, string id)
            => security.GetPermissions(StorageActor.User(id));

        public static StoragePermissions GetGroupPermissions(this IStorageSecurity security, string id)
            => security.GetPermissions(StorageActor.Group(id));

        public static IStorageSecurity UpdatePublicPermissions(this IStorageSecurity security, StoragePermissions permissions)
            => security.UpdatePermissions(StorageActor.Public, permissions);

        public static IStorageSecurity UpdateAuthenticatedPermissions(this IStorageSecurity security, StoragePermissions permissions)
            => security.UpdatePermissions(StorageActor.Authenticated, permissions);

        public static IStorageSecurity UpdateUserPermissions(this IStorageSecurity security, string id, StoragePermissions permissions)
            => security.UpdatePermissions(StorageActor.User(id), permissions);

        public static IStorageSecurity UpdateGroupPermissions(this IStorageSecurity security, string id, StoragePermissions permissions)
            => security.UpdatePermissions(StorageActor.Group(id), permissions);
    }
}