using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Storage.Tasks
{
    public struct ConfiguredObservableOperationAwaitable
    {
        public ConfiguredValueTaskAwaitable Awaitable { get; }

        public IProgress<StorageOperationProgress>? Progress { get; }

        public ConfiguredObservableOperationAwaitable(in ConfiguredValueTaskAwaitable awaitable, IProgress<StorageOperationProgress>? progress)
        {
            Awaitable = awaitable;
            Progress = progress;
        }
    }

    public struct ConfiguredObservableOperationAwaitable<T>
    {
        public ConfiguredValueTaskAwaitable<T> Awaitable { get; }

        public IProgress<StorageOperationProgress>? Progress { get; }

        public ConfiguredObservableOperationAwaitable(in ConfiguredValueTaskAwaitable<T> awaitable, IProgress<StorageOperationProgress>? progress)
        {
            Awaitable = awaitable;
            Progress = progress;
        }
    }
}