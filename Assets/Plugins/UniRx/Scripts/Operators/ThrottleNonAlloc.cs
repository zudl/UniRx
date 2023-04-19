using System;

namespace UniRx.Operators
{
    internal class ThrottleNonAllocObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan dueTime;
        readonly TimeSpan timeStep;
        readonly IScheduler scheduler;

        public ThrottleNonAllocObservable(IObservable<T> source, TimeSpan dueTime, TimeSpan timeStep, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.timeStep = timeStep;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleNonAlloc(this, observer, cancel).Run();
        }

        class ThrottleNonAlloc : OperatorObserverBase<T, T>
        {
            readonly ThrottleNonAllocObservable<T> parent;
            readonly object gate = new object();
            DateTimeOffset next;
            T latestValue = default(T);
            bool hasValue = false;
            IDisposable timer;

            public ThrottleNonAlloc(ThrottleNonAllocObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                timer = CreateTimer();
                var subscription = parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(timer, subscription);
            }

            IDisposable CreateTimer()
            {
                var periodicScheduler = parent.scheduler as ISchedulerPeriodic;
                if (periodicScheduler != null)
                {
                    return periodicScheduler.SchedulePeriodic(parent.timeStep, Tick);
                }

                return parent.scheduler.Schedule(parent.timeStep, TickRecursive);
            }

            void Tick()
            {
                var shouldEmit = false;
                lock (gate)
                {
                    if (hasValue && parent.scheduler.Now >= next)
                    {
                        hasValue = false;
                        shouldEmit = true;
                    }
                }

                if (shouldEmit) {
                    observer.OnNext(latestValue);
                }
            }

            void TickRecursive(Action<TimeSpan> self)
            {
                Tick();
                self(parent.timeStep);
            }

            public override void OnNext(T value)
            {
                lock (gate)
                {
                    hasValue = true;
                    latestValue = value;
                    next = parent.scheduler.Now.Add(parent.dueTime);
                }
            }

            public override void OnError(Exception error)
            {
                timer.Dispose();

                lock (gate)
                {
                    hasValue = false;
                    try { observer.OnError(error); } finally { Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                timer.Dispose();

                lock (gate)
                {
                    if (hasValue)
                    {
                        observer.OnNext(latestValue);
                    }
                    hasValue = false;
                    try { observer.OnCompleted(); } finally { Dispose(); }
                }
            }
        }
    }
}
