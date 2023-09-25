using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NCoreUtils.Storage.Tasks
{
    public sealed class ObservableOperationTask
    {
        public Task Task { get; }

        public Progress<StorageOperationProgress>? Progress { get; }

        public ObservableOperationTask(Task task, Progress<StorageOperationProgress>? progress)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Progress = progress;
        }

        public ConfiguredObservableOperationTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredObservableOperationTaskAwaitable(Task.ConfigureAwait(continueOnCapturedContext), Progress);

        public TaskAwaiter GetAwaiter()
            => Task.GetAwaiter();
    }

    public sealed class ObservableOperationTask<T>
    {
        public Task<T> Task { get; }

        public Progress<StorageOperationProgress>? Progress { get; }

        public ObservableOperationTask(Task<T> task, Progress<StorageOperationProgress>? progress)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Progress = progress;
        }

        public ConfiguredObservableOperationTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredObservableOperationTaskAwaitable<T>(Task.ConfigureAwait(continueOnCapturedContext), Progress);

        public TaskAwaiter<T> GetAwaiter()
            => Task.GetAwaiter();
    }
}