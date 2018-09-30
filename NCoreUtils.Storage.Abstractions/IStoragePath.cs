using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Storage
{
    public interface IStoragePath
    {
        IStorageRoot StorageRoot { get; }

        string Name { get; }

        Uri Uri { get; }

        Task<IStoragePath> GetParentAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}