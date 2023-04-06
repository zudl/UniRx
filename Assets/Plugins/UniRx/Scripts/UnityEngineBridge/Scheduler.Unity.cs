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

        public static DateTimeOffset NowFromUnityTime(float time)
        {
            lock (Gate)
            {
                if (_appStartTime == default(DateTimeOffset))
                {
                    _appStartTime = DateTimeOffset.Now - TimeSpan.FromSeconds(Time.unscaledTime);
                }
            }
            return _appStartTime + TimeSpan.FromSeconds(time);
        }

        public static void SetDefaultForUnity()
        {
            Scheduler.DefaultSchedulers.ConstantTimeOperations = Scheduler.Immediate;
            Scheduler.DefaultSchedulers.TailRecursion = Scheduler.Immediate;
            Scheduler.DefaultSchedulers.Iteration = Scheduler.CurrentThread;
            Scheduler.DefaultSchedulers.TimeBasedOperations = MainThread;
            Scheduler.DefaultSchedulers.AsyncConversions = Scheduler.ThreadPool;
        }
    }
}
