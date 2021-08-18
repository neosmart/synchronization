using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoSmart.Synchronization;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class ScopedMutexTests
    {
        [TestMethod]
        public void SameNameLocks()
        {
            var guid = Guid.NewGuid();
            using var lock1 = TimerAssertions.Within(10, () => ScopedMutex.Create(guid.ToString()));
            Assert.ThrowsException<TimeoutException>(delegate
            {
                using var lock2 = TimerAssertions.Within(25, () => ScopedMutex.Create(guid.ToString()));
            });
        }

        [TestMethod]
        public void DifferentNameDoesntLock()
        {
            using var lock1 = TimerAssertions.Within(10, () => ScopedMutex.Create(Guid.NewGuid().ToString()));
            using var lock2 = TimerAssertions.Within(25, () => ScopedMutex.Create(Guid.NewGuid().ToString()));
        }

        [TestMethod]
        public async Task SameNameLocksAsync()
        {
            var guid = Guid.NewGuid();

            using var lock1 = await TimerAssertions.WithinAsync(10, () => ScopedMutex.CreateAsync(guid.ToString()));
            await Assert.ThrowsExceptionAsync<TimeoutException>(async delegate
            {
                using var lock2 = await TimerAssertions.WithinAsync(25, () => ScopedMutex.CreateAsync(guid.ToString()));
            });
        }

        [TestMethod]
        public async Task DifferentNameDoesntLockAsync()
        {
            using var lock1 = await TimerAssertions.WithinAsync(100, () => ScopedMutex.CreateAsync(Guid.NewGuid().ToString()));
            using var lock2 = await TimerAssertions.WithinAsync(200, () => ScopedMutex.CreateAsync(Guid.NewGuid().ToString()));
        }
    }
}
