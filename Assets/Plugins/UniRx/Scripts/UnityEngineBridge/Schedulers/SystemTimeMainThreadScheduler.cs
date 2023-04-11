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
        /// <summary>
        /// Schedules units of work to run on the Unity main thread during Update phase, uses system time.
        /// </summary>
        /// <remarks>
        /// SystemTimeMainThreadScheduler.Now represents current system time.
        /// This is the default scheduler for Timestamped() and Timer(DateTimeOffset) operators.
        /// </remarks>
        sealed class SystemTimeMainThreadScheduler : UpdateMainThreadSchedulerBase
        {
            public override DateTimeOffset Now
            {
                get { return Scheduler.Now; }
            }

            protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                var endTime = Now + dueTime;
                do
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;
                } while (Now < endTime);

                MainThreadDispatcher.UnsafeSend(action);
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
