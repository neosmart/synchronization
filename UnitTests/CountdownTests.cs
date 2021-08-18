using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using CountdownEvent = NeoSmart.Synchronization.CountdownEvent;

namespace UnitTests
{
    [TestClass]
    public class CountdownTests
    {
        [TestMethod]
        public void FiresAtZero()
        {
            using var countdown = new CountdownEvent(3);
            TimerAssertions.Within(100, () =>
            {
                Assert.IsFalse(countdown.Wait(0), "Countdown signalled without reaching zero");
                countdown.Signal();
                Assert.IsFalse(countdown.Wait(0), "Countdown signalled without reaching zero");
                countdown.Signal();
                Assert.IsFalse(countdown.Wait(0), "Countdown signalled without reaching zero");
                countdown.Signal();
                Assert.IsTrue(countdown.Wait(0), "Countdown not signalled after reaching zero");
            });
        }

        [TestMethod]
        public void CountMatchesSignalCount()
        {
            using var countdown = new CountdownEvent(3);
            TimerAssertions.Within(100, () =>
            {
                Assert.AreEqual(3, countdown.Count);
                countdown.Signal();
                Assert.AreEqual(2, countdown.Count);
                countdown.Signal();
                Assert.AreEqual(1, countdown.Count);
                countdown.Signal();
                Assert.AreEqual(0, countdown.Count);
            });
        }

        [TestMethod]
        public void InitialCountOnInit()
        {
            using var countdown = new CountdownEvent(3);
            Assert.AreEqual(3, countdown.InitialCount);
        }

        [TestMethod]
        public void CountAfterReset()
        {
            using var countdown = new CountdownEvent(3);
            countdown.Reset(2);
            Assert.AreEqual(2, countdown.Count);
        }

        [TestMethod]
        public void InitialCountAfterReset()
        {
            using var countdown = new CountdownEvent(3);
            countdown.Reset(2);
            Assert.AreEqual(2, countdown.InitialCount);
        }

        [TestMethod]
        public void ThrowsOnExtraSignal()
        {
            using var countdown = new CountdownEvent(1);
            countdown.Signal();
            Assert.ThrowsException<InvalidOperationException>(() => countdown.Signal());
        }

        [TestMethod]
        public async Task ThrowsOnDanglingWaitAsync()
        {
            var countdown = new CountdownEvent(1);
            var task = countdown.WaitAsync();

            countdown.Dispose();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async delegate { await task; });
        }
    }
}
