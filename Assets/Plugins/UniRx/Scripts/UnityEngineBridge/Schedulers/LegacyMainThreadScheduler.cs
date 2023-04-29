﻿using System;
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
        /// Legacy main thread scheduler.
        /// </summary>
        /// <remarks>
        /// This implementation has issues with time-based operators such as Delay.
        /// Use MainThreadScheduler instead.
        /// </remarks>
        sealed class LegacyMainThreadScheduler : UpdateMainThreadSchedulerBase
        {
            public override DateTimeOffset Now
            {
                get { return Scheduler.Now; }
            }

            protected override IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
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