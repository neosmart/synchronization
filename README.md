# NeoSmart.Synchronization

A cross-platform, async-friendly synchronization library for .NET and .NET Core. Available on Nuget as `NeoSmart.Synchronization`.

## `ScopedMutex`

The `ScopedMutex` object is a key-based mutex - a hybrid between a named `Mutex` and a `lock` block. Unlike a `lock`, instead of locking on an object, it locks on a unique identifier (GUID, usually). Unlike a named `Mutex`, the lock is automatically released in case of exception, request abortion, etc.

Since it uses a GUID, it will block across sessions and page requests, and will be "deleted" when either all references to it cease to exist, or the ASP.NET application is restarted. It is intended to be used almost exclusively in `using` blocks.

* `ScopedMutex::Create(string name)`
* `ScopedMutex::CreateAsync(string name)`

`ScopedMutex` is a RAII wrapper around an underlying semaphore and does not expose the equivalent of `.Lock()` or `.Release()` - instantiation of the `ScopedMutex` guarantees exclusive access for the duration of the `ScopedMutex` lifetime. It's therefore of critical importance that a `ScopedMutex` is correctly disposed and we recommend initializing it exclusively with `using` statements.

Usage is very simple:

```csharp
using (var mutex = ScopedMutex::Create("myguid"))
{
    // Exclusive access granted, any other calls to Create()
    // or CreateAsync() will block until `mutex` is disposed!

    foo.DoSomethingCritical();
}
```

In an async context, `ScopedMutex::CreateAsync()` should be used instead - make sure not to forget to await the result of the call, though! For example:

```csharp
using (var mutex = await ScopedMutex::CreateAsync("myguid")
{
    // Exclusive access granted
}
```

Should the code terminate or throw an exception for any reason in the middle of the `using` block before the mutex is disposed, the `using` will automatically release the named mutex and make it available for other threads/requests. This holds true regardless of whether the request is terminated due to user abort, an exception in the `//Do something here` block, etc.

It is important to note that `ScopedMutex` does not throw an `AbandonedMutexException`: if another thread/request holding the `ScopedMutex` with the same name/GUID aborts, the next instantiation of a `ScopedMutex` with the same name will succeed without notice that anything went wrong.

## `CountdownEvent`

The `NeoSmart.Synchronization.CountdownEvent` class is an async-friendly analog to the deprecated `System.Threading.CountdownEvent` type. Unlike the one in the standard library, this one exposes a `WaitAsync()` method (and its overlads taking a combination of one or more of `TimeSpan timeout`, `int milliseconds`, and `CancellationToken cancel`).

It is a lightweight synchronization type that lets one initialize a `CountdownEvent` with some count of pending events, say the number of jobs left in a queue. It can then be given to each worker to signal (by calling `CountdownEvent.Signal()`) effectively decrementing the internal count. One or more observers waiting on the completion of dispatched tasks can use `CountdownEvent.Wait()` or `CountdownEvent.WaitAsync()` to wait for all worker threads to finish.
