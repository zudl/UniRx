using System;
using UniRx.Operators;

namespace UniRx
{
    // Non allocating versions of time based operators
    public static partial class Observable
    {
        static TimeSpan _defaultStepwiseOperatorAccuracy = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// Default step used by stepping time operators such as ThrottleNonAlloc.
        /// </summary>
        public static TimeSpan DefaultStepwiseOperatorAccuracy
        {
            get { return _defaultStepwiseOperatorAccuracy; }
            set { _defaultStepwiseOperatorAccuracy = value; }
        }

        /// <summary>
        /// A Throttle implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard Throttle operator which allocates a new scheduler subscription on every OnNext call,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with precision of Observable.DefaultStepwiseOperatorAccuracy,
        /// limited by frame duration in case of main thread schedulers.
        /// </remarks>
        public static IObservable<TSource> ThrottleNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            return source.ThrottleNonAlloc(dueTime, _defaultStepwiseOperatorAccuracy, Scheduler.DefaultSchedulers.TimeBasedOperations);
        }

        /// <summary>
        /// A Throttle implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard Throttle operator which allocates a new scheduler subscription on every OnNext call,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with precision of Observable.DefaultStepwiseOperatorAccuracy,
        /// limited by frame duration in case of main thread schedulers.
        /// </remarks>
        public static IObservable<TSource> ThrottleNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            return source.ThrottleNonAlloc(dueTime, _defaultStepwiseOperatorAccuracy, scheduler);
        }

        /// <summary>
        /// A Throttle implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard Throttle operator which allocates a new scheduler subscription on every OnNext call,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with timeStep accuracy.
        /// </remarks>
        /// <param name="timeStep">Internal timer period</param>
        public static IObservable<TSource> ThrottleNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, TimeSpan timeStep)
        {
            return source.ThrottleNonAlloc(dueTime, timeStep, Scheduler.DefaultSchedulers.TimeBasedOperations);
        }

        /// <summary>
        /// A Throttle implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard Throttle operator which allocates a new scheduler subscription on every OnNext call,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with timeStep accuracy.
        /// </remarks>
        /// <param name="timeStep">Internal timer period</param>
        public static IObservable<TSource> ThrottleNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, TimeSpan timeStep, IScheduler scheduler)
        {
            return new ThrottleNonAllocObservable<TSource>(source, dueTime, timeStep, scheduler);
        }

        /// <summary>
        /// A ThrottleFirst implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard ThrottleFirst operator which allocates a new scheduler subscription for every emitted value,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with precision of Observable.DefaultStepwiseOperatorAccuracy,
        /// limited by frame duration in case of main thread schedulers.
        /// </remarks>
        public static IObservable<TSource> ThrottleFirstNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            return source.ThrottleFirstNonAlloc(dueTime, _defaultStepwiseOperatorAccuracy, Scheduler.DefaultSchedulers.TimeBasedOperations);
        }

        /// <summary>
        /// A ThrottleFirst implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard ThrottleFirst operator which allocates a new scheduler subscription for every emitted value,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with precision of Observable.DefaultStepwiseOperatorAccuracy,
        /// limited by frame duration in case of main thread schedulers.
        /// </remarks>
        public static IObservable<TSource> ThrottleFirstNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            return source.ThrottleFirstNonAlloc(dueTime, _defaultStepwiseOperatorAccuracy, scheduler);
        }

        /// <summary>
        /// A ThrottleFirst implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard ThrottleFirst operator which allocates a new scheduler subscription for every emitted value,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with timeStep accuracy.
        /// </remarks>
        /// <param name="timeStep">Internal timer period</param>
        public static IObservable<TSource> ThrottleFirstNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, TimeSpan timeStep)
        {
            return source.ThrottleFirstNonAlloc(dueTime, timeStep, Scheduler.DefaultSchedulers.TimeBasedOperations);
        }

        /// <summary>
        /// A ThrottleFirst implementation that doesn't allocate memory after subscription.
        /// </summary>
        /// <remarks>
        /// Unlike standard ThrottleFirst operator which allocates a new scheduler subscription for every emitted value,
        /// this implementation uses single scheduler subscription allocated on Subscribe.
        /// Time intervals are measured with timeStep accuracy.
        /// </remarks>
        /// <param name="timeStep">Internal timer period</param>
        public static IObservable<TSource> ThrottleFirstNonAlloc<TSource>(this IObservable<TSource> source, TimeSpan dueTime, TimeSpan timeStep, IScheduler scheduler)
        {
            return new ThrottleFirstNonAllocObservable<TSource>(source, dueTime, timeStep, scheduler);
        }
    }
}
