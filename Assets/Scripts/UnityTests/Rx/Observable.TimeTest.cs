using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniRx.Tests
{

    public class ObservableTimeTest
    {
        [SetUp]
        public void Init()
        {
            TestUtil.SetScehdulerForImport();
        }

        [TearDown]
        public void Dispose()
        {
            UniRx.Scheduler.SetDefaultForUnity();
        }

        [Test]
        public void TimerTest()
        {
            // periodic(Observable.Interval)
            {
                var now = Scheduler.ThreadPool.Now;
                var xs = Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
                    .Take(3)
                    .Timestamp()
                    .ToArray()
                    .Wait();

                xs[0].Value.Is(0L);
                (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();

                xs[1].Value.Is(1L);
                (now.AddMilliseconds(1800) <= xs[1].Timestamp && xs[1].Timestamp <= now.AddMilliseconds(2200)).IsTrue();

                xs[2].Value.Is(2L);
                (now.AddMilliseconds(2800) <= xs[2].Timestamp && xs[2].Timestamp <= now.AddMilliseconds(3200)).IsTrue();
            }

            // dueTime + periodic
            {
                var now = Scheduler.ThreadPool.Now;
                var xs = Observable.Timer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1))
                    .Take(3)
                    .Timestamp()
                    .Select(x => Math.Round((x.Timestamp - now).TotalSeconds, 0))
                    .ToArray()
                    .Wait();

                xs[0].Is(2);
                xs[1].Is(3);
                xs[2].Is(4);
            }

            // dueTime(DateTimeOffset)
            {
                var now = Scheduler.ThreadPool.Now;
                var xs = Observable.Timer(now.AddSeconds(2), TimeSpan.FromSeconds(1))
                    .Take(3)
                    .Timestamp()
                    .Select(x => Math.Round((x.Timestamp - now).TotalSeconds, 0))
                    .ToArray()
                    .Wait();

                xs[0].Is(2);
                xs[1].Is(3);
                xs[2].Is(4);
            }

            // onetime
            {
                var now = Scheduler.ThreadPool.Now;
                var xs = Observable.Timer(TimeSpan.FromSeconds(2))
                    .Timestamp()
                    .Select(x => Math.Round((x.Timestamp - now).TotalSeconds, 0))
                    .ToArray()
                    .Wait();

                xs[0].Is(2);
            }

            // non periodic scheduler
            {
                var now = Scheduler.CurrentThread.Now;
                var xs = Observable.Timer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), Scheduler.CurrentThread)
                    .Take(3)
                    .Timestamp()
                    .Select(x => Math.Round((x.Timestamp - now).TotalSeconds, 0))
                    .ToArray()
                    .Wait();

                xs[0].Is(2);
                xs[1].Is(3);
                xs[2].Is(4);
            }
        }

        [Test]
        public void SampleTest()
        {
            // 2400, 4800, 7200, 9600
            var xs = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                .Take(10)
                .Sample(TimeSpan.FromMilliseconds(2400), Scheduler.CurrentThread)
                .Timestamp()
                .ToArray()
                .Wait();

            xs[0].Value.Is(2);
            xs[1].Value.Is(4);
            xs[2].Value.Is(7);
            xs[3].Value.Is(9);
        }

        [Test]
        public void TimeoutTest()
        {
            var xs = Observable.Concat(
                    Observable.Return(1).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(9).Delay(TimeSpan.FromSeconds(3))
                )
                .Timestamp()
                .Timeout(TimeSpan.FromMilliseconds(1500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(2);
            xs[0].Value.Value.Is(1);
            xs[1].Exception.IsInstanceOf<TimeoutException>();
        }

        [Test]
        public void TimeoutTestOffset()
        {
            var now = Scheduler.ThreadPool.Now;
            var xs = Observable.Concat(
                    Observable.Return(1).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(9).Delay(TimeSpan.FromSeconds(3))
                )
                .Timestamp()
                .Timeout(now.AddMilliseconds(3500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(3);
            xs[0].Value.Value.Is(1);
            xs[1].Value.Value.Is(5);
            xs[2].Exception.IsInstanceOf<TimeoutException>();
        }

        [Test]
        public void ThrottleTest()
        {
            var xs = Observable.Concat(
                    Observable.Return(1).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(2).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(3).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(4).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(6).Delay(TimeSpan.FromSeconds(3)), // over 2500
                    Observable.Return(7).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(8).Delay(TimeSpan.FromSeconds(1)) // with onCompleted
                )
                .Timestamp()
                .Throttle(TimeSpan.FromMilliseconds(2500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(3);
            xs[0].Value.Value.Is(5);
            xs[1].Value.Value.Is(8);
            xs[2].Kind.Is(NotificationKind.OnCompleted);
        }

        [Test]
        public void ThrottleNonAllocTest()
        {
            var xs = Observable.Concat(
                    Observable.Return(1).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(2).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(3).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(4).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(2)),
                    Observable.Return(6).Delay(TimeSpan.FromSeconds(3)), // over 2500
                    Observable.Return(7).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(8).Delay(TimeSpan.FromSeconds(1)) // with onCompleted
                )
                .Timestamp()
                .ThrottleNonAlloc(TimeSpan.FromMilliseconds(2500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(3);
            xs[0].Value.Value.Is(5);
            xs[1].Value.Value.Is(8);
            xs[2].Kind.Is(NotificationKind.OnCompleted);
        }

        [Test]
        public void ThrottleFirstTest()
        {
            var xs = Observable.Concat(
                    Observable.Return(1),
                    Observable.Return(2).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(3).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(4).Delay(TimeSpan.FromSeconds(0.4)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(0.2)), // over 2500
                    Observable.Return(6).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(7).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(8).Delay(TimeSpan.FromSeconds(1)), // over 2500
                    Observable.Return(9) // withCompleted
                )
                .Timestamp()
                .ThrottleFirst(TimeSpan.FromMilliseconds(2500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(4);
            xs[0].Value.Value.Is(1);
            xs[1].Value.Value.Is(5);
            xs[2].Value.Value.Is(8);
            xs[3].Kind.Is(NotificationKind.OnCompleted);
        }

        [Test]
        public void ThrottleFirstNonAllocTest()
        {
            var xs = Observable.Concat(
                    Observable.Return(1),
                    Observable.Return(2).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(3).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(4).Delay(TimeSpan.FromSeconds(0.4)),
                    Observable.Return(5).Delay(TimeSpan.FromSeconds(0.2)), // over 2500
                    Observable.Return(6).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(7).Delay(TimeSpan.FromSeconds(1)),
                    Observable.Return(8).Delay(TimeSpan.FromSeconds(1)), // over 2500
                    Observable.Return(9) // withCompleted
                )
                .Timestamp()
                .ThrottleFirstNonAlloc(TimeSpan.FromMilliseconds(2500))
                .Materialize()
                .ToArray()
                .Wait();

            xs.Length.Is(4);
            xs[0].Value.Value.Is(1);
            xs[1].Value.Value.Is(5);
            xs[2].Value.Value.Is(8);
            xs[3].Kind.Is(NotificationKind.OnCompleted);
        }

        [Test]
        public void Timestamp()
        {
            var xs = Observable.Concat(
                Observable.Return(1),
                Observable.Return(2).Delay(TimeSpan.FromSeconds(1)),
                Observable.Return(3).Delay(TimeSpan.FromSeconds(1)),
                Observable.Return(4).Delay(TimeSpan.FromSeconds(0.4)),
                Observable.Return(5).Delay(TimeSpan.FromSeconds(0.2)) // over 2500
            ).Timestamp()
            .ToArray()
            .Wait();

            var now = DateTime.Now;

            xs[0].Value.Is(1); (now - xs[0].Timestamp).TotalSeconds.Is(x => 2.5 <= x && x <= 3.0);
            xs[1].Value.Is(2); (now - xs[1].Timestamp).TotalSeconds.Is(x => 1.4 <= x && x <= 1.8);
            xs[2].Value.Is(3); (now - xs[2].Timestamp).TotalSeconds.Is(x => 0.5 <= x && x <= 0.8);
            xs[3].Value.Is(4); (now - xs[3].Timestamp).TotalSeconds.Is(x => 0.18 <= x && x <= 0.3);
            xs[4].Value.Is(5); (now - xs[4].Timestamp).TotalSeconds.Is(x => 0 <= x && x <= 0.1);
        }

        [Test]
        public void TimeInterval()
        {
            var xs = Observable.Concat(
                Observable.Return(1),
                Observable.Return(2).Delay(TimeSpan.FromSeconds(1)),
                Observable.Return(3).Delay(TimeSpan.FromSeconds(1)),
                Observable.Return(4).Delay(TimeSpan.FromSeconds(0.4)),
                Observable.Return(5).Delay(TimeSpan.FromSeconds(0.2)) // over 2500
            ).TimeInterval()
            .ToArray()
            .Wait();

            xs[0].Value.Is(1); xs[0].Interval.TotalSeconds.Is(x => 0.0 <= x && x <= 0.1);
            xs[1].Value.Is(2); xs[1].Interval.TotalSeconds.Is(x => 0.9 <= x && x <= 1.1);
            xs[2].Value.Is(3); xs[2].Interval.TotalSeconds.Is(x => 0.9 <= x && x <= 1.1);
            xs[3].Value.Is(4); xs[3].Interval.TotalSeconds.Is(x => 0.3 <= x && x <= 0.5);
            xs[4].Value.Is(5); xs[4].Interval.TotalSeconds.Is(x => 0.15 <= x && x <= 0.25);
        }

        [Test]
        public void DelayTest()
        {
            var now = Scheduler.ThreadPool.Now;

            var xs = Observable.Range(1, 3)
                .Delay(TimeSpan.FromSeconds(1))
                .Timestamp()
                .ToArray()
                .Wait();

            xs[0].Value.Is(1);
            (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();

            xs[1].Value.Is(2);
            (now.AddMilliseconds(800) <= xs[1].Timestamp && xs[1].Timestamp <= now.AddMilliseconds(1200)).IsTrue();

            xs[2].Value.Is(3);
            (now.AddMilliseconds(800) <= xs[2].Timestamp && xs[2].Timestamp <= now.AddMilliseconds(1200)).IsTrue();
        }

        [UnityTest]
        public IEnumerator ScaledDelayTest()
        {
            const float Period = 2f;
            const float HighTimeScale = 2f;
            const float LowTimeScale = 0.5f;
            const float ErrorMs = 200;
            var scaledScheduler = Scheduler.MainThread;

            Time.timeScale = HighTimeScale;
            var timeOnStart = Time.unscaledTime;

            yield return null;

            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(ms => ms > Period / HighTimeScale - ErrorMs && ms < Period / HighTimeScale + ErrorMs);

            Time.timeScale = LowTimeScale;
            timeOnStart = Time.unscaledTime;

            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();

            elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(ms => ms > Period / LowTimeScale - ErrorMs && ms < Period / LowTimeScale + ErrorMs);

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator UnscaledDelayTest()
        {
            const float Period = 2f;
            const float HighTimeScale = 2f;
            const float LowTimeScale = 0.5f;
            const float ErrorMs = 200;
            var unscaledScheduler = Scheduler.MainThreadIgnoreTimeScale;

            Time.timeScale = HighTimeScale;
            var timeOnStart = Time.unscaledTime;

            yield return null;

            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(ms => ms > Period - ErrorMs && ms < Period + ErrorMs);

            Time.timeScale = LowTimeScale;
            timeOnStart = Time.unscaledTime;

            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();

            elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(ms => ms > Period - ErrorMs && ms < Period + ErrorMs);

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator DateTimeOffsetTimerTest()
        {
            const float TimeScale = 2f;
            var realtimeScheduler = Scheduler.MainThreadSystemTime;
            var scaledScheduler = Scheduler.MainThread;

            var timeOnStart = DateTimeOffset.Now;
            var dueTime = TimeSpan.FromSeconds(1f);
            var period = TimeSpan.FromSeconds(0.5f);
            var error = TimeSpan.FromSeconds(0.2f);

            Time.timeScale = TimeScale;

            yield return null;

            Timestamped<long>[] emittedValues = null;
            yield return Observable.Timer(timeOnStart + dueTime, period, realtimeScheduler, scaledScheduler)
                .Take(10)
                .Timestamp()
                .ToArray()
                .Do(values => emittedValues = values)
                .ToAwaitableEnumerator();

            Time.timeScale = 1f;

            var prevTime = timeOnStart;
            for (var i = 0; i < emittedValues.Length; i++)
            {
                var expectedDelta = i == 0 ? dueTime : TimeSpan.FromSeconds(period.TotalSeconds / TimeScale);
                var expectedTime = prevTime + expectedDelta;
                prevTime = expectedTime;
                emittedValues[i].Timestamp.Is(t => t > expectedTime - error && t < expectedTime + error);
            }

        }

        [UnityTest]
        public IEnumerator ThrottleFirstEndOfFrameTest()
        {
            yield return null;
            var period = TimeSpan.FromSeconds(1f);
            long[] emittedValues = null;
            yield return Observable.Interval(period, Scheduler.MainThread)
                .Take(4)
                .ThrottleFirst(period, Scheduler.MainThreadEndOfFrame)
                .ToArray()
                .Do(array => emittedValues = array)
                .ToAwaitableEnumerator();

            emittedValues.Length.Is(2);
        }

        [UnityTest]
        public IEnumerator ThrottleFirstNonAllocEndOfFrameTest()
        {
            yield return null;
            var period = TimeSpan.FromSeconds(1f);
            yield return Observable.Interval(period, Scheduler.MainThread)
                .Take(4)
                .ThrottleFirst(period, Scheduler.MainThreadEndOfFrame)
                .ToArray()
                .Do(array => array.Length.Is(2))
                .ToAwaitableEnumerator();
        }
    }
}
