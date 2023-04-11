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
        /// Uses custom time source provided in constructor.
        /// Schedules units of work to run on the Unity main thread during Update phase.
        /// </summary>
        /// <remarks>
        /// CustomMainThreadScheduler.Now represents time from the custom source.
        /// Use instead of the SystemTimeMainThreadScheduler if you have a preferred time source.
        /// </remarks>
        sealed class CustomMainThreadScheduler : UpdateMainThreadSchedulerBase
        {
            readonly Func<DateTimeOffset> _timeGetter;

            public CustomMainThreadScheduler(Func<DateTimeOffset> timeGetter)
            {
                _timeGetter = timeGetter;
            }

            public override DateTimeOffset Now
            {
                get { return _timeGetter.Invoke(); }
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
