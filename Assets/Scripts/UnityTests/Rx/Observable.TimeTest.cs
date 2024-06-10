using System;
using System.Collections;
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
        public void DelayCompleteTest()
        {
            var now = Scheduler.ThreadPool.Now;
            var xs = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .Take(4)
                .Where(c => c < 3)
                .Delay(TimeSpan.FromSeconds(1))
                .Materialize()
                .Timestamp()
                .ToArray()
                .Wait();

            xs[0].Value.Kind.Is(NotificationKind.OnNext);
            xs[0].Value.Value.Is(0);
            xs[1].Value.Kind.Is(NotificationKind.OnNext);
            xs[1].Value.Value.Is(1);
            xs[2].Value.Kind.Is(NotificationKind.OnNext);
            xs[2].Value.Value.Is(2);
            xs[3].Value.Kind.Is(NotificationKind.OnCompleted);
            (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();
            (now.AddMilliseconds(1300) <= xs[1].Timestamp && xs[1].Timestamp <= now.AddMilliseconds(1700)).IsTrue();
            (now.AddMilliseconds(1800) <= xs[2].Timestamp && xs[2].Timestamp <= now.AddMilliseconds(2200)).IsTrue();
            (now.AddMilliseconds(2300) <= xs[3].Timestamp && xs[3].Timestamp <= now.AddMilliseconds(2700)).IsTrue();
        }

        [Test]
        public void DelayErrorTest()
        {
            var now = Scheduler.ThreadPool.Now;
            var xs = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .Take(4)
                .Do(c =>
                {
                    if (c == 3)
                    {
                        throw new Exception();
                    }
                })
                .Delay(TimeSpan.FromSeconds(1))
                .Materialize()
                .Timestamp()
                .ToArray()
                .Wait();

            (xs.Length <= 3).IsTrue();
            xs[xs.Length - 1].Value.Kind.Is(NotificationKind.OnError);
            (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();
            (now.AddMilliseconds(1300) <= xs[xs.Length - 1].Timestamp && xs[xs.Length - 1].Timestamp <= now.AddMilliseconds(1700)).IsTrue();
        }

        [UnityTest]
        public IEnumerator ScaledDownDelayTest()
        {
            const float Period = 2f;
            const float TimeScale = 0.5f;
            const float Error = 0.2f;
            var scaledScheduler = Scheduler.MainThread;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period / TimeScale - Error && s < Period / TimeScale + Error);
        }

        [UnityTest]
        public IEnumerator ScaledUpDelayTest()
        {
            const float Period = 2f;
            const float TimeScale = 2f;
            const float Error = 0.2f;
            var scaledScheduler = Scheduler.MainThread;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period / TimeScale - Error && s < Period / TimeScale + Error);
        }

        [UnityTest]
        public IEnumerator UnscaledDownDelayTest()
        {
            const float Period = 2f;
            const float TimeScale = 0.5f;
            const float Error = 0.2f;
            var unscaledScheduler = Scheduler.MainThreadIgnoreTimeScale;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period - Error && s < Period + Error);
        }

        [UnityTest]
        public IEnumerator UnscaledUpDelayTest()
        {
            const float Period = 2f;
            const float TimeScale = 2f;
            const float Error = 0.2f;
            var unscaledScheduler = Scheduler.MainThreadIgnoreTimeScale;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period - Error && s < Period + Error);
        }

        [Test]
        public void DelayNonAllocCompleteTest()
        {
            var now = Scheduler.ThreadPool.Now;
            var xs = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .Take(4)
                .Where(c => c < 3)
                .DelayNonAlloc(TimeSpan.FromSeconds(1))
                .Materialize()
                .Timestamp()
                .ToArray()
                .Wait();

            xs[0].Value.Kind.Is(NotificationKind.OnNext);
            xs[0].Value.Value.Is(0);
            xs[1].Value.Kind.Is(NotificationKind.OnNext);
            xs[1].Value.Value.Is(1);
            xs[2].Value.Kind.Is(NotificationKind.OnNext);
            xs[2].Value.Value.Is(2);
            xs[3].Value.Kind.Is(NotificationKind.OnCompleted);
            (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();
            (now.AddMilliseconds(1300) <= xs[1].Timestamp && xs[1].Timestamp <= now.AddMilliseconds(1700)).IsTrue();
            (now.AddMilliseconds(1800) <= xs[2].Timestamp && xs[2].Timestamp <= now.AddMilliseconds(2200)).IsTrue();
            (now.AddMilliseconds(2300) <= xs[3].Timestamp && xs[3].Timestamp <= now.AddMilliseconds(2700)).IsTrue();
        }

        [Test]
        public void DelayNonAllocErrorTest()
        {
            var now = Scheduler.ThreadPool.Now;
            var xs = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .Take(4)
                .Do(c =>
                {
                    if (c == 3)
                    {
                        throw new Exception();
                    }
                })
                .DelayNonAlloc(TimeSpan.FromSeconds(1))
                .Materialize()
                .Timestamp()
                .ToArray()
                .Wait();

            (xs.Length <= 3).IsTrue();
            xs[0].Value.Kind.Is(NotificationKind.OnNext);
            xs[xs.Length - 1].Value.Kind.Is(NotificationKind.OnError);
            (now.AddMilliseconds(800) <= xs[0].Timestamp && xs[0].Timestamp <= now.AddMilliseconds(1200)).IsTrue();
            (now.AddMilliseconds(1300) <= xs[xs.Length - 1].Timestamp && xs[xs.Length - 1].Timestamp <= now.AddMilliseconds(1700)).IsTrue();
        }


        [UnityTest]
        public IEnumerator ScaledDownDelayNonAllocTest()
        {
            const float Period = 2f;
            const float TimeScale = 0.5f;
            const float Error = 0.2f;
            var scaledScheduler = Scheduler.MainThread;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .DelayNonAlloc(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period / TimeScale - Error && s < Period / TimeScale + Error);
        }

        [UnityTest]
        public IEnumerator ScaledUpDelayNonAllocTest()
        {
            const float Period = 2f;
            const float TimeScale = 2f;
            const float Error = 0.2f;
            var scaledScheduler = Scheduler.MainThread;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .DelayNonAlloc(TimeSpan.FromSeconds(Period), scaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period / TimeScale - Error && s < Period / TimeScale + Error);
        }

        [UnityTest]
        public IEnumerator UnscaledDownDelayNonAllocTest()
        {
            const float Period = 2f;
            const float TimeScale = 0.5f;
            const float Error = 200;
            var unscaledScheduler = Scheduler.MainThreadIgnoreTimeScale;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .DelayNonAlloc(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period - Error && s < Period + Error);
        }

        [UnityTest]
        public IEnumerator UnscaledUpDelayNonAllocTest()
        {
            const float Period = 2f;
            const float TimeScale = 2f;
            const float Error = 0.2f;
            var unscaledScheduler = Scheduler.MainThreadIgnoreTimeScale;

            yield return null;
            Time.timeScale = TimeScale;
            var timeOnStart = Time.unscaledTime;
            yield return Observable.ReturnUnit()
                .DelayNonAlloc(TimeSpan.FromSeconds(Period), unscaledScheduler)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            var elapsed = Time.unscaledTime - timeOnStart;
            elapsed.Is(s => s > Period - Error && s < Period + Error);
        }

        [UnityTest]
        public IEnumerator DateTimeOffsetTimerTest()
        {
            const float TimeScale = 2f;
            const float DueTime = 1f;
            const float Period = 0.5f;
            const float Error = 0.2f;
            var realtimeScheduler = Scheduler.MainThreadSystemTime;
            var scaledScheduler = Scheduler.MainThread;
            var timeOnStart = DateTimeOffset.Now;

            yield return null;
            Time.timeScale = TimeScale;
            Timestamped<long>[] emittedValues = null;
            yield return Observable.Timer(timeOnStart.AddSeconds(DueTime), TimeSpan.FromSeconds(Period), realtimeScheduler, scaledScheduler)
                .Take(4)
                .Timestamp()
                .ToArray()
                .Do(values => emittedValues = values)
                .ToAwaitableEnumerator();
            Time.timeScale = 1f;

            emittedValues[0].Timestamp.Is(t => Math.Abs((t - timeOnStart).TotalSeconds - 1f) < Error); // SystemTimeMainThreadScheduler is default for dueTo
            emittedValues[1].Timestamp.Is(t => Math.Abs((t - timeOnStart).TotalSeconds - 1.25f) < Error); // MainThreadScheduler is default for period
            emittedValues[2].Timestamp.Is(t => Math.Abs((t - timeOnStart).TotalSeconds - 1.5f) < Error);
            emittedValues[3].Timestamp.Is(t => Math.Abs((t - timeOnStart).TotalSeconds - 1.75f) < Error);
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
            emittedValues[0].Is(0);
            emittedValues[1].Is(2);
        }

        [UnityTest]
        public IEnumerator ThrottleFirstNonAllocEndOfFrameTest()
        {
            yield return null;
            var period = TimeSpan.FromSeconds(1f);
            long[] emittedValues = null;
            yield return Observable.Interval(period, Scheduler.MainThread)
                .Take(4)
                .ThrottleFirstNonAlloc(period, Scheduler.MainThreadEndOfFrame)
                .ToArray()
                .Do(array => emittedValues = array)
                .ToAwaitableEnumerator();

            emittedValues.Length.Is(2);
            emittedValues[0].Is(0);
            emittedValues[1].Is(2);
        }
    }
}
