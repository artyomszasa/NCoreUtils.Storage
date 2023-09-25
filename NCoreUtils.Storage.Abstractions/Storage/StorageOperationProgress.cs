namespace NCoreUtils.Storage
{
    public struct StorageOperationProgress
    {
        public long StepsPerformed { get; }

        public long? StepsTotal { get; }

        public StorageOperationProgress(long stepsPerformed, long? stepsTotal)
        {
            StepsPerformed = stepsPerformed;
            StepsTotal = stepsTotal;
        }
    }
}