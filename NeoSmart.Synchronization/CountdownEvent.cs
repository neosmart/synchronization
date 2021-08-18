using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.Synchronization
{
    public class CountdownEvent : IDisposable
    {
#if NET5_0_OR_GREATER
        private TaskCompletionSource _task;
#else
        private TaskCompletionSource<bool> _task;
#endif
        private int _count;
        public int Count => _count;
        public int InitialCount { get; private set; }

        public CountdownEvent(int count)
        {
            _task = new();
            Reset(count);
        }

        public bool IsSet => _count == 0;

        public void Reset(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            InitialCount = count;
            Interlocked.Exchange(ref _count, count);
            if (_task.Task.IsCompleted)
            {
                _task = new();
            }
        }

        public void Signal()
        {
            var decremented = Interlocked.Decrement(ref _count);
            if (decremented == 0)
            {
#if NET5_0_OR_GREATER
                _task.SetResult();
#else
                _task.SetResult(true);
#endif
            }
            else if (decremented < 0)
            {
                throw new InvalidOperationException($"{nameof(CountdownEvent)}.{nameof(Signal)} called more times than allowed!");
            }
        }

        public void Reset() => Reset(InitialCount);

        public void Dispose()
        {
            _task.TrySetException(new InvalidOperationException("CountdownEvent was disposed with waiters waiting"));
        }

        public void Wait() => _task.Task.Wait();
        public bool Wait(int milliseconds) => _task.Task.Wait(milliseconds);
        public bool Wait(int milliseconds, CancellationToken cancel) => _task.Task.Wait(milliseconds, cancel);
        public bool Wait(TimeSpan timeout) => _task.Task.Wait(timeout);
        public bool Wait(TimeSpan timeout, CancellationToken cancel) => _task.Task.Wait((int)timeout.TotalMilliseconds, cancel);

        public Task WaitAsync() => _task.Task;
        public async Task WaitAsync(int milliseconds)
        {
            if ((await Task.WhenAny(_task.Task, Task.Delay(milliseconds))) != _task.Task)
            {
                throw new OperationCanceledException();
            }
        }
        public async Task WaitAsync(int milliseconds, CancellationToken cancel)
        {
            if ((await Task.WhenAny(_task.Task, Task.Delay(milliseconds, cancel))) != _task.Task)
            {
                throw new OperationCanceledException();
            }
        }
        public async Task WaitAsync(TimeSpan timeout)
        {
            if ((await Task.WhenAny(_task.Task, Task.Delay(timeout))) != _task.Task)
            {
                throw new OperationCanceledException();
            }
        }
        public async Task WaitAsync(TimeSpan timeout, CancellationToken cancel)
        {
            if ((await Task.WhenAny(_task.Task, Task.Delay(timeout, cancel))) != _task.Task)
            {
                throw new OperationCanceledException();
            }
        }
    }
}
