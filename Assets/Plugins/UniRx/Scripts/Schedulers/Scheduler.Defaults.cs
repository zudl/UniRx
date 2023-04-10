namespace UniRx
{
#if UniRxLibrary
    public static partial class SchedulerUnity
    {
#else
    public static partial class Scheduler
    {
#endif
        // configurable defaults
        public static class DefaultSchedulers
        {
            static IScheduler constantTime;
            public static IScheduler ConstantTimeOperations
            {
                get
                {
                    return constantTime ?? (constantTime = Scheduler.Immediate);
                }
                set
                {
                    constantTime = value;
                }
            }

            static IScheduler tailRecursion;
            public static IScheduler TailRecursion
            {
                get
                {
                    return tailRecursion ?? (tailRecursion = Scheduler.Immediate);
                }
                set
                {
                    tailRecursion = value;
                }
            }

            static IScheduler iteration;
            public static IScheduler Iteration
            {
                get
                {
                    return iteration ?? (iteration = Scheduler.CurrentThread);
                }
                set
                {
                    iteration = value;
                }
            }

            static IScheduler timeBasedOperations;
            public static IScheduler TimeBasedOperations
            {
                get
                {
#if UniRxLibrary
                    return timeBasedOperations ?? (timeBasedOperations = Scheduler.ThreadPool);
#else
                    return timeBasedOperations ?? (timeBasedOperations = Scheduler.MainThread); // MainThread as default for TimeBased Operation
#endif
                }
                set
                {
                    timeBasedOperations = value;
                }
            }

            static IScheduler realTimeOperations;
            public static IScheduler RealTimeOperations
            {
                get
                {
#if UniRxLibrary
                    return realTimeOperations ?? (realTimeOperations = Scheduler.ThreadPool);
#else
                    return realTimeOperations ?? (realTimeOperations = Scheduler.MainThreadSystemTime);
#endif
                }
                set
                {
                    realTimeOperations = value;
                }
            }

            static IScheduler asyncConversions;
            public static IScheduler AsyncConversions
            {
                get
                {
#if WEB_GL
                    // WebGL does not support threadpool
                    return asyncConversions ?? (asyncConversions = Scheduler.MainThread);
#else
                    return asyncConversions ?? (asyncConversions = Scheduler.ThreadPool);
#endif
                }
                set
                {
                    asyncConversions = value;
                }
            }

            public static void SetDotNetCompatible()
            {
                ConstantTimeOperations = Scheduler.Immediate;
                TailRecursion = Scheduler.Immediate;
                Iteration = Scheduler.CurrentThread;
                TimeBasedOperations = Scheduler.ThreadPool;
                RealTimeOperations = Scheduler.ThreadPool;
                AsyncConversions = Scheduler.ThreadPool;
            }
        }

        static IScheduler mainThread;

        /// <summary>
        /// Unity native MainThread Queue Scheduler. Run on mainthread and delayed on coroutine update loop, elapsed time is calculated based on Time.time.
        /// </summary>
        public static IScheduler MainThread
        {
            get
            {
                return mainThread ?? (mainThread = new MainThreadScheduler());
            }
        }

        static IScheduler mainThreadIgnoreTimeScale;

        /// <summary>
        /// Another MainThread scheduler, delay elapsed time is calculated based on Time.unscaledDeltaTime.
        /// </summary>
        public static IScheduler MainThreadIgnoreTimeScale
        {
            get
            {
                return mainThreadIgnoreTimeScale ?? (mainThreadIgnoreTimeScale = new IgnoreTimeScaleMainThreadScheduler());
            }
        }

        static IScheduler mainThreadSystemTime;

        /// <summary>
        /// Another MainThread scheduler, elapsed time is calculated based on system time
        /// </summary>
        public static IScheduler MainThreadSystemTime
        {
            get
            {
                return mainThreadSystemTime ?? (mainThreadSystemTime = new SystemTimeMainThreadScheduler());
            }
        }

        static IScheduler mainThreadFixedUpdate;

        /// <summary>
        /// Run on fixed update mainthread, delay elapsed time is calculated based on Time.fixedTime.
        /// </summary>
        public static IScheduler MainThreadFixedUpdate
        {
            get
            {
                return mainThreadFixedUpdate ?? (mainThreadFixedUpdate = new FixedUpdateMainThreadScheduler());
            }
        }

        static IScheduler mainThreadEndOfFrame;

        /// <summary>
        /// Run on end of frame mainthread, delay elapsed time is calculated based on Time.deltaTime.
        /// </summary>
        public static IScheduler MainThreadEndOfFrame
        {
            get
            {
                return mainThreadEndOfFrame ?? (mainThreadEndOfFrame = new EndOfFrameMainThreadScheduler());
            }
        }
    }
}
