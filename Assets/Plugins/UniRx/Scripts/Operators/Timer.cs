using System;

namespace UniRx.Operators
{
    internal class TimerObservable : OperatorObservableBase<long>
    {
        readonly DateTimeOffset? _dueTimeA;
        readonly TimeSpan? _dueTimeB;
        readonly TimeSpan? _period;
        readonly IScheduler _dueTimeScheduler;
        readonly IScheduler _periodScheduler;

        public TimerObservable(DateTimeOffset dueTime, TimeSpan? period, IScheduler dueTimeScheduler, IScheduler periodScheduler)
            : base(dueTimeScheduler == Scheduler.CurrentThread)
        {
            this._dueTimeA = dueTime;
            this._period = period.HasValue ? Scheduler.Normalize(period.Value) : (TimeSpan?) null;
            this._dueTimeScheduler = dueTimeScheduler;
            this._periodScheduler = periodScheduler;
        }

        public TimerObservable(TimeSpan dueTime, TimeSpan? period, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this._dueTimeB = Scheduler.Normalize(dueTime);
            this._period = period.HasValue ? Scheduler.Normalize(period.Value) : (TimeSpan?) null;
            this._dueTimeScheduler = scheduler;
            this._periodScheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<long> observer, IDisposable cancel)
        {
            var timerObserver = new Timer(observer, cancel);

            var dueTime = (_dueTimeA != null)
                ? Scheduler.Normalize(_dueTimeA.Value - _dueTimeScheduler.Now)
                : _dueTimeB.Value;

            // one-shot
            if (_period == null)
            {
                return _dueTimeScheduler.Schedule(Scheduler.Normalize(dueTime), () =>
                {
                    timerObserver.OnNext();
                    timerObserver.OnCompleted();
                });
            }

            if (_dueTimeScheduler == _periodScheduler)
            {
                return ScheduleCycle(dueTime, _period.Value, _periodScheduler, timerObserver);
            }

            var disposable = new SerialDisposable();
            disposable.Disposable = _dueTimeScheduler.Schedule(dueTime, () =>
            {
                timerObserver.OnNext();  // run first
                disposable.Disposable = ScheduleCycle(_period.Value, _period.Value, _periodScheduler, timerObserver);
            });
            return disposable;
        }

        static IDisposable ScheduleCycle(TimeSpan dueTime, TimeSpan period, IScheduler scheduler, Timer timer)
        {
            var periodicScheduler = scheduler as ISchedulerPeriodic;
            if (periodicScheduler != null && dueTime == period)
            {
                return periodicScheduler.SchedulePeriodic(period, timer.OnNext);
            }
            return scheduler.Schedule(dueTime, self =>
            {
                timer.OnNext();
                self(period);
            });
        }

        class Timer : OperatorObserverBase<long, long>
        {
            long index = 0;

            public Timer(IObserver<long> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public void OnNext()
            {
                try
                {
                    base.observer.OnNext(index++);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public override void OnNext(long value)
            {
                // no use.
            }

            public override void OnError(Exception error)
            {
                try { observer.OnError(error); }
                finally { Dispose(); }
            }

            public override void OnCompleted()
            {
                try { observer.OnCompleted(); }
                finally { Dispose(); }
            }
        }
    }
}
