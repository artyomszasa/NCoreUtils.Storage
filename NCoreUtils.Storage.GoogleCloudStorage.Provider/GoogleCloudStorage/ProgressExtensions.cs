using System.Runtime.CompilerServices;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    static class ProgressExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTotal(this IProgress progress, decimal value)
        {
            if (null != progress)
            {
                progress.Total = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(this IProgress progress, decimal value)
        {
            if (null != progress)
            {
                progress.Value = value;
            }
        }
    }
}