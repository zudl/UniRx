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
        class FixedUpdateMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            public FixedUpdateMainThreadScheduler()
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
                    var startTime = Time.fixedTime;
                    var dt = (float)dueTime.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        var elapsed = Time.fixedTime - startTime;
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
                    var startTime = Time.fixedTime;
                    var dt = (float)period.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        var ft = Time.fixedTime;
                        var elapsed = ft - startTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            startTime = ft;
                        }
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return Scheduler.NowFromUnityTime(Time.fixedTime); }
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

                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(DelayAction(time, action, d));

                return d;
            }

            public IDisposable SchedulePeriodic(TimeSpan period, Action action)
            {
                var d = new BooleanDisposable();
                var time = Scheduler.Normalize(period);

                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(PeriodicAction(time, action, d));

                return d;
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(ImmediateAction(state, action, cancel));
            }
        }
    }
}
