using System;
using System.Collections;

namespace UniRx
{
#if UniRxLibrary
    public static partial class SchedulerUnity
    {
#else
    public static partial class Scheduler
    {
#endif
        sealed class SystemTimeMainThreadScheduler : UpdateMainThreadSchedulerBase
        {
            public sealed override DateTimeOffset Now
            {
                get { return Scheduler.Now; }
            }

            protected sealed override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                var endTime = Now + dueTime;
                do
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;
                } while (Now < endTime);

                MainThreadDispatcher.UnsafeSend(action);
            }

            protected sealed override IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
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

                var endTime = Now;
                while (true)
                {
                    endTime += period;
                    do
                    {
                        yield return null;
                        if (cancellation.IsDisposed) yield break;
                    } while (Now < endTime);

                    MainThreadDispatcher.UnsafeSend(action);
                }
            }
        }
    }
}
