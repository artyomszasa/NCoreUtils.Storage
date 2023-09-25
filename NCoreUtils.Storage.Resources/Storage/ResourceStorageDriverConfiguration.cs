using System.Collections.Generic;
using System.Reflection;
using NCoreUtils.Storage.Resources;

namespace NCoreUtils.Storage
{
    public class ResourceStorageDriverConfiguration : List<Assembly>, IResourceStorageDriverConfiguration { }
}