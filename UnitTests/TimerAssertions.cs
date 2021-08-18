using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    public class TimeoutException : Exception
    {
    }

    public static class TimerAssertions
    {
        private static ConcurrentBag<Thread> Threads = new();
        private static BlockingCollection<(TimeSpan Timeout, Func<object?> Action, TaskCompletionSource<object?> Result)> WorkQueue = new();

        static TimerAssertions()
        {
            for (int i = 0; i < 16; ++i)
            {
                AddThread();
            }
        }

        private static void AddThread()
        {
            bool suppressed = ExecutionContext.IsFlowSuppressed();
            if (!suppressed)
            {
                ExecutionContext.SuppressFlow();
            }

            try
            {
                var thread = new Thread(WorkerEntryPoint)
                {
                    IsBackground = true,
                };
                thread.Start();
                Threads.Add(thread);
            }
            finally
            {
                if (!suppressed)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }

        public static void Within(long milliseconds, Action action) => Within(TimeSpan.FromMilliseconds(milliseconds), action);

        public static void Within(TimeSpan timeout, Action action)
        {
            var task = new TaskCompletionSource<object?>();
            WorkQueue.Add((timeout, () => { action(); return true; }, task));
            task.Task.GetAwaiter().GetResult();
        }

        public static R Within<R>(long milliseconds, Func<R> action) => Within(TimeSpan.FromMilliseconds(milliseconds), action);

        public static R Within<R>(TimeSpan timeout, Func<R> action)
        {
            var task = new TaskCompletionSource<object?>();
            WorkQueue.Add((timeout, () => action(), task));
            return (R) task.Task.GetAwaiter().GetResult()!;
        }

        public static Task<R> WithinAsync<R>(long milliseconds, Func<Task<R>> action) => WithinAsync(TimeSpan.FromMilliseconds(milliseconds), action);
        public static async Task<R> WithinAsync<R>(TimeSpan timeout, Func<Task<R>> action)
        {
            var task = action();
            if ((await Task.WhenAny(task, Task.Delay(timeout))) != task)
            {
                // Timeout
                throw new TimeoutException();
            }
            return task.GetAwaiter().GetResult();
        }

        private static void WorkerEntryPoint()
        {
            while (true)
            {
                var job = WorkQueue.Take();
                using var cancel = new CancellationTokenSource(job.Timeout);
                bool pending = true;
                var currentThread = Thread.CurrentThread;
                cancel.Token.Register(delegate
                {
                    if (pending)
                    {
                        currentThread.Interrupt();
                    }
                });

                try
                {
                    job.Result.SetResult(job.Action());
                }
                catch (ThreadInterruptedException)
                {
                    job.Result.SetException(new TimeoutException());
                }
                finally
                {
                    pending = false;
                }
            }
        }
    }
}
