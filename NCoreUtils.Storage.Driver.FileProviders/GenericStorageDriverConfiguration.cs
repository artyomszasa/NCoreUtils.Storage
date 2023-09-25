using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;

namespace NCoreUtils.Storage.FileProviders
{
    public class GenericStorageDriverConfiguration
        : ConcurrentDictionary<string, IFileProvider>
        , IGenericStorageDriverConfiguration
    { }
}