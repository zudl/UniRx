using System;
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

        static readonly object Gate = new object();
        static DateTimeOffset _appStartTime;

        public static DateTimeOffset NowFromUnityTime(double time)
        {
            lock (Gate)
            {
                if (_appStartTime == default(DateTimeOffset))
                {
#if UNITY_2020_2_OR_NEWER
                    var timeSinceAppStart = TimeSpan.FromSeconds(Time.unscaledTimeAsDouble);
#else
                    var timeSinceAppStart = TimeSpan.FromSeconds(Time.unscaledTime);
#endif
                    _appStartTime = DateTimeOffset.Now - timeSinceAppStart;
                }
            }
            return _appStartTime + TimeSpan.FromSeconds(time);
        }

        public static void SetDefaultForUnity()
        {
            Scheduler.DefaultSchedulers.ConstantTimeOperations = Scheduler.Immediate;
            Scheduler.DefaultSchedulers.TailRecursion = Scheduler.Immediate;
            Scheduler.DefaultSchedulers.Iteration = Scheduler.CurrentThread;
            Scheduler.DefaultSchedulers.TimeBasedOperations = Scheduler.MainThread;
            Scheduler.DefaultSchedulers.RealTimeOperations = Scheduler.MainThreadSystemTime;
            Scheduler.DefaultSchedulers.AsyncConversions = Scheduler.ThreadPool;
        }
    }
}
