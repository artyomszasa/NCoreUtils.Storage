using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Storage.Tasks
{
    public sealed class ConfiguredObservableOperationTaskAwaitable
    {
        public ConfiguredTaskAwaitable Awaitable { get; }

        public IProgress<StorageOperationProgress>? Progress { get; }

        public ConfiguredObservableOperationTaskAwaitable(in ConfiguredTaskAwaitable awaitable, IProgress<StorageOperationProgress>? progress)
        {
            Awaitable = awaitable;
            Progress = progress;
        }
    }

    public sealed class ConfiguredObservableOperationTaskAwaitable<T>
    {
        public ConfiguredTaskAwaitable<T> Awaitable { get; }

        public IProgress<StorageOperationProgress>? Progress { get; }

        public ConfiguredObservableOperationTaskAwaitable(in ConfiguredTaskAwaitable<T> awaitable, IProgress<StorageOperationProgress>? progress)
        {
            Awaitable = awaitable;
            Progress = progress;
        }
    }
}