using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MapModeFramework
{
    public static class TaskHandler
    {
        public static readonly Queue<Func<CancellationToken, Task>> taskQueue = new Queue<Func<CancellationToken, Task>>();
        public static Task currentTask;

        public static bool IsBusy => currentTask?.IsCompleted == false;
        private static CancellationTokenSource cancelTokenSource;
        private static readonly object queueLock = new object();

        public static void StartQueue(Func<CancellationToken, Task> task)
        {
            lock (queueLock)
            {
                taskQueue.Enqueue(task);
            }
            if (!IsBusy)
            {
                ProcessQueuedTasksAsync().ConfigureAwait(false);
            }
        }

        private static async Task ProcessQueuedTasksAsync()
        {
            while (true)
            {
                Func<CancellationToken, Task> taskFunc = null;
                lock (queueLock)
                {
                    if (taskQueue.Count == 0)
                    {
                        break;
                    }
                    taskFunc = taskQueue.Dequeue();
                }
                using CancellationTokenSource localSource = CancellationTokenSource.CreateLinkedTokenSource(GetOrGenerateToken());
                CancellationToken token = localSource.Token;
                try
                {
                    currentTask = taskFunc(token);
                    await currentTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Core.Error($"Error while running asynchronous task: {ex.Message}");
                }
            }
            currentTask = null;
            DisposeCancellationTokenSource();
        }

        private static CancellationToken GetOrGenerateToken()
        {
            cancelTokenSource ??= new CancellationTokenSource();
            return cancelTokenSource.Token;
        }

        private static void DisposeCancellationTokenSource()
        {
            cancelTokenSource?.Dispose();
            cancelTokenSource = null;
        }

        public static void KillQueue()
        {
            lock (queueLock)
            {
                cancelTokenSource?.Cancel();
                DisposeCancellationTokenSource();
                taskQueue.Clear();
            }
        }
    }
}
