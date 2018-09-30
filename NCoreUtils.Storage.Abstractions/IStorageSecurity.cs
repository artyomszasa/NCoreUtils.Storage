using System.Collections.Generic;

namespace NCoreUtils.Storage
{
    public interface IStorageSecurity : IEnumerable<KeyValuePair<StorageActor, StoragePermissions>>
    {
        StoragePermissions GetPermissions(StorageActor actor);

        IStorageSecurity UpdatePermissions(StorageActor actor, StoragePermissions permissions);
    }
}