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
        class MainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            readonly Action<object> _scheduleAction;

            public MainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
                _scheduleAction = new Action<object>(Schedule);
            }

            // delay action is run in StartCoroutine
            // Okay to action run synchronous and guaranteed run on MainThread
            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null; // not immediately, run next frame
                }
                else
                {
                    yield return new WaitForSeconds((float)dueTime.TotalSeconds);
                }

                if (cancellation.IsDisposed) yield break;
                MainThreadDispatcher.UnsafeSend(action);
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
                    var seconds = (float)(period.TotalMilliseconds / 1000.0);
                    var yieldInstruction = new WaitForSeconds(seconds); // cache single instruction object

                    while (true)
                    {
                        yield return yieldInstruction;
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return Scheduler.NowFromUnityTime(Time.time); }
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

            void ScheduleQueueing<T>(object state)
            {
                var t = (Tuple<ICancelable, T, Action<T>>)state;
                if (!t.Item1.IsDisposed)
                {
                    t.Item3(t.Item2);
                }
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
