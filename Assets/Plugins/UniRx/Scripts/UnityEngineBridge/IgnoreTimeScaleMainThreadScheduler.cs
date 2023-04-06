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

        class IgnoreTimeScaleMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            readonly Action<object> _scheduleAction;

            public IgnoreTimeScaleMainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
                _scheduleAction = new Action<object>(Schedule);
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

                        elapsed += Time.unscaledDeltaTime;
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
                        yield return null; // not immediately, run next frame
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

                        elapsed += Time.unscaledDeltaTime;
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
                get { return Scheduler.NowFromUnityTime(Time.unscaledTime); }
            }

            void Schedule(object state)
            {
                var t = (Tuple<BooleanDisposable, Action>)state;
                if (!t.Item1.IsDisposed)
                {
                    t.Item2();
                }
            }

            public IDisposable Schedule(Action action)
            {
                var d = new BooleanDisposable();
                MainThreadDispatcher.Post(_scheduleAction, Tuple.Create(d, action));
                return d;
            }

            public IDisposable Schedule(DateTimeOffset dueTime, Action action)
            {
                return Schedule(dueTime - Now, action);
            }

            public IDisposable Schedule(TimeSpan dueTime, Action action)
            {
                var d = new BooleanDisposable();
                var time = Scheduler.Normalize(dueTime);

                MainThreadDispatcher.SendStartCoroutine(DelayAction(time, action, d));

                return d;
            }

            public IDisposable SchedulePeriodic(TimeSpan period, Action action)
            {
                var d = new BooleanDisposable();
                var time = Scheduler.Normalize(period);

                MainThreadDispatcher.SendStartCoroutine(PeriodicAction(time, action, d));

                return d;
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.Post(QueuedAction<T>.Instance, Tuple.Create(cancel, state, action));
            }

            static class QueuedAction<T>
            {
                public static readonly Action<object> Instance = new Action<object>(Invoke);

                public static void Invoke(object state)
                {
                    var t = (Tuple<ICancelable, T, Action<T>>)state;

                    if (!t.Item1.IsDisposed)
                    {
                        t.Item3(t.Item2);
                    }
                }
            }
        }
    }
}
