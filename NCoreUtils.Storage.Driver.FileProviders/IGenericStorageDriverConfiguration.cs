using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace NCoreUtils.Storage.FileProviders
{
    public interface IGenericStorageDriverConfiguration : IReadOnlyDictionary<string, IFileProvider> { }
}