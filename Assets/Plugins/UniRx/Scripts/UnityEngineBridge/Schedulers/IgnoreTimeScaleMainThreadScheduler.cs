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

        class IgnoreTimeScaleMainThreadScheduler : UpdateMainThreadSchedulerBase
        {

            public override DateTimeOffset Now
            {
                get { return Scheduler.NowFromUnityTime(Time.unscaledTime); }
            }

            protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
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

            protected override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
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
    }
}
