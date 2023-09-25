using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.Tasks
{
    public struct ObservableOperation
    {
        public ValueTask Task { get; }

        public Progress<StorageOperationProgress>? Progress { get; }

        public ObservableOperation(in ValueTask task, Progress<StorageOperationProgress>? progress = default)
        {
            Task = task;
            Progress = progress;
        }

        public ObservableOperation(Task task, Progress<StorageOperationProgress>? progress = default)
            : this(new ValueTask(task), progress)
        { }

        public ObservableOperationTask AsTask()
            => new ObservableOperationTask(Task.AsTask(), Progress);

        public ConfiguredObservableOperationAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredObservableOperationAwaitable(Task.ConfigureAwait(continueOnCapturedContext), Progress);

        public ValueTaskAwaiter GetAwaiter()
            => Task.GetAwaiter();
    }

    public struct ObservableOperation<T>
    {
        public ValueTask<T> Task { get; }

        public Progress<StorageOperationProgress>? Progress { get; }

        public ObservableOperation(T result)
            : this(new ValueTask<T>(result))
        { }

        public ObservableOperation(in ValueTask<T> task, Progress<StorageOperationProgress>? progress = default)
        {
            Task = task;
            Progress = progress;
        }

        public ObservableOperation(Task<T> task, Progress<StorageOperationProgress>? progress = default)
            : this(new ValueTask<T>(task), progress)
        { }

        public void Deconstruct(out ValueTask<T> task, out Progress<StorageOperationProgress>? progress)
        {
            task = Task;
            progress = Progress;
        }

        public ObservableOperationTask<T> AsTask()
            => new ObservableOperationTask<T>(Task.AsTask(), Progress);

        public ConfiguredObservableOperationAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredObservableOperationAwaitable<T>(Task.ConfigureAwait(continueOnCapturedContext), Progress);

        public ValueTaskAwaiter<T> GetAwaiter()
            => Task.GetAwaiter();
    }
}