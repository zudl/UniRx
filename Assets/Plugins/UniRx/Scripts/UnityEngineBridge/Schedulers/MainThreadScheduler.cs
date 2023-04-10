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
        /// Schedules units of work to run on the Unity main thread during Update phase.
        /// </summary>
        /// <remarks>
        /// The behaviour of the Now property has been updated in order to fix time-based operators:
        /// MainThreadScheduler.Now represents scaled Unity time and should only be accessed by Rx operators.
        /// To get current system time, use SystemTimeMainThreadScheduler.Now instead.
        /// To get the old behavior, use LegacyMainThreadScheduler.
        /// </remarks>
        sealed class MainThreadScheduler : UpdateMainThreadSchedulerBase
        {
            public sealed override DateTimeOffset Now
            {
                get { return Scheduler.NowFromUnityTime(Time.time); }
            }

            protected sealed override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
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
    }
}
