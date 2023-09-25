using System.Collections.Generic;
using System.Reflection;

namespace NCoreUtils.Storage.Resources
{
    public interface IResourceStorageDriverConfiguration : IEnumerable<Assembly> { }
}