using Microsoft.Extensions.Logging;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    class LoggerFeature : ILoggerFeature
    {
        public ILogger GetLogger(IStorageProvider storageProvider) => ((StorageProvider)storageProvider).Logger;
    }
}