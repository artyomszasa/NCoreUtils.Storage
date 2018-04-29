using NCoreUtils.Progress;

namespace NCoreUtils.Storage.FileSystem
{
    public class DummyProgress : IProgress
    {
        public decimal Total { get; set; } = 0;
        public decimal Value { get; set; } = 0;
    }
}