using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    static class ProgressExtensions
    {
        public static void SetTotal(this IProgress progress, decimal value)
        {
            if (null != progress)
            {
                progress.Total = value;
            }
        }

        public static void SetValue(this IProgress progress, decimal value)
        {
            if (null != progress)
            {
                progress.Value = value;
            }
        }
    }
}