using System;

namespace UniRx.Operators
{
    internal class ThrottleFirstNonAllocObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan dueTime;
        readonly TimeSpan timeStep;
        readonly IScheduler scheduler;

        public ThrottleFirstNonAllocObservable(IObservable<T> source, TimeSpan dueTime, TimeSpan timeStep, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.timeStep = timeStep;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFirstNonAlloc(this, observer, cancel).Run();
        }

        class ThrottleFirstNonAlloc : OperatorObserverBase<T, T>
        {
            readonly ThrottleFirstNonAllocObservable<T> parent;
            readonly object gate = new object();
            DateTimeOffset? closedUntil;
            IDisposable timer;

            public ThrottleFirstNonAlloc(ThrottleFirstNonAllocObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
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
                lock (gate)
                {
                    if (closedUntil.HasValue && parent.scheduler.Now > closedUntil.Value)
                    {
                        closedUntil = null;
                    }
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
                    if (closedUntil.HasValue) return;
                    closedUntil = parent.scheduler.Now + parent.dueTime;
                    observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                timer.Dispose();

                lock (gate)
                {
                    try { observer.OnError(error); } finally { Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                timer.Dispose();

                lock (gate)
                {
                    try { observer.OnCompleted(); } finally { Dispose(); }
                }
            }
        }
    }
}
