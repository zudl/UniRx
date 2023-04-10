using System;
using System.Collections;
using UnityEngine;

namespace UniRx
{
#if UniRxLibrary
    public static partial class SchedulerUnity
    {
#else
    public static partial class Scheduler
    {
#endif
        /// <summary>
        /// Schedules units of work to run on the Unity main thread during the End of frame phase.
        /// </summary>
        /// <remarks>
        /// The behaviour of the Now property has been updated in order to fix time-based operators:
        /// EndOfFrameMainThreadScheduler.Now represents scaled Unity time and should only be accessed by Rx operators.
        /// To get current system time, use SystemTimeMainThreadScheduler.Now instead.
        /// </remarks>
        class EndOfFrameMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            public EndOfFrameMainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
            }

            IEnumerator ImmediateAction<T>(T state, Action<T> action, ICancelable cancellation)
            {
                yield return null;
                if (cancellation.IsDisposed) yield break;

                MainThreadDispatcher.UnsafeSend(action, state);
            }

            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;

                    MainThreadDispatcher.UnsafeSend(action);
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)dueTime.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        elapsed += Time.deltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            break;
                        }
                    }
                }
            }

            IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (period == TimeSpan.Zero)
                {
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)period.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        elapsed += Time.deltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            elapsed = 0;
                        }
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return Scheduler.NowFromUnityTime(Time.time); }
            }

            public IDisposable Schedule(Action action)
            {
                return Schedule(TimeSpan.Zero, action);
            }

            public IDisposable Schedule(DateTimeOffset dueTime, Action action)
            {
                return Schedule(dueTime - Now, action);
            }

            public IDisposable Schedule(TimeSpan dueTime, Action action)
            {
                var d = new BooleanDisposable();
                var time = Scheduler.Normalize(dueTime);

                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(DelayAction(time, action, d));

                return d;
            }

            public IDisposable SchedulePeriodic(TimeSpan period, Action action)
            {
                var d = new BooleanDisposable();
                var time = Scheduler.Normalize(period);

                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(PeriodicAction(time, action, d));

                return d;
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(ImmediateAction(state, action, cancel));
            }
        }
    }
}
