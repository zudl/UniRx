using System;
using System.Collections.Generic;

namespace UniRx.Operators
{
    internal class DelayNonAllocObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> _source;
        readonly TimeSpan _dueTime;
        readonly TimeSpan _timeStep;
        readonly IScheduler _scheduler;

        public DelayNonAllocObservable(IObservable<T> source, TimeSpan dueTime, TimeSpan timeStep, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            _source = source;
            _dueTime = dueTime;
            _timeStep = timeStep;
            _scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DelayNonAlloc(this, observer, cancel).Run();
        }

        class DelayNonAlloc : OperatorObserverBase<T, T>
        {
            readonly DelayNonAllocObservable<T> _parent;
            readonly object _gate = new object();
            Queue<Timestamped<T>> _queue;
            TimeSpan _delay;
            DateTimeOffset? _completeAt;
            IDisposable _sourceSubscription;
            IDisposable _timerSubscription;

            public DelayNonAlloc(DelayNonAllocObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                _queue = new Queue<Timestamped<T>>();
                _completeAt = null;
                _delay = Scheduler.Normalize(_parent._dueTime);

                _timerSubscription = CreateTimer();
                var sourceSubscription = new SingleAssignmentDisposable();
                _sourceSubscription = sourceSubscription;
                sourceSubscription.Disposable = _parent._source.Subscribe(this);
                return StableCompositeDisposable.Create(_timerSubscription, _sourceSubscription);
            }

            public override void OnNext(T value)
            {
                var next = _parent._scheduler.Now.Add(_delay);

                lock (_gate)
                {
                    _queue.Enqueue(new Timestamped<T>(value, next));
                }
            }

            public override void OnError(Exception error)
            {
                _timerSubscription.Dispose();
                _sourceSubscription.Dispose();

                lock (_gate)
                {
                    ForwardOnError(error);
                }
            }

            public override void OnCompleted()
            {
                _sourceSubscription.Dispose();

                var next = _parent._scheduler.Now.Add(_delay);

                lock (_gate)
                {
                    _completeAt = next;
                }
            }

            IDisposable CreateTimer()
            {
                var periodicScheduler = _parent._scheduler as ISchedulerPeriodic;
                if (periodicScheduler != null)
                {
                    return periodicScheduler.SchedulePeriodic(_parent._timeStep, Tick);
                }

                return _parent._scheduler.Schedule(_parent._timeStep, TickRecursive);
            }

            void Tick()
            {
                lock (_gate)
                {
                    if (_queue.Count > 0)
                    {
                        if (_queue.Peek().Timestamp <= _parent._scheduler.Now)
                        {
                            ForwardOnNext(_queue.Dequeue().Value);
                        }
                        return;
                    }

                    if (_completeAt.HasValue && _completeAt.Value <= _parent._scheduler.Now)
                    {
                        ForwardOnCompleted();
                    }
                }
            }

            void TickRecursive(Action<TimeSpan> self)
            {
                Tick();
                self(_parent._timeStep);
            }

            void ForwardOnNext(T value) {
                observer.OnNext(value);
            }

            void ForwardOnCompleted() {
                try {
                    _timerSubscription.Dispose();
                    observer.OnCompleted();
                }
                finally {
                    Dispose();
                }
            }

            void ForwardOnError(Exception error) {
                _queue.Clear();
                try {
                    observer.OnError(error);
                }
                finally {
                    Dispose();
                }
            }
        }
    }
}
