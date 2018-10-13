using Microsoft.Extensions.Logging;

namespace NCoreUtils.Storage.Features
{
    // TODO: move to interface on next interface update.
    public interface ILoggerFeature
    {
        ILogger GetLogger(IStorageProvider storageProvider);
    }
}