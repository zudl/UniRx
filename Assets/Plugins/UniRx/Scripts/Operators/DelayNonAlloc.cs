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
            bool _isStopped;
            bool _isRunningOnNext;
            bool _errorOccuredWhileRunningOnNext;
            Exception _error;
            bool _hasSourceCompleted;
            DateTimeOffset _completeAt;
            IDisposable _sourceSubscription;
            IDisposable _timerSubscription;

            public DelayNonAlloc(DelayNonAllocObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                _queue = new Queue<Timestamped<T>>();
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
                _sourceSubscription.Dispose();

                lock (_gate)
                {
                    if (_isRunningOnNext)
                    {
                        _error = error;
                        _errorOccuredWhileRunningOnNext = true;
                        return;
                    }
                }

                ForwardOnErrorAndStop(error);
            }

            public override void OnCompleted()
            {
                _sourceSubscription.Dispose();

                var next = _parent._scheduler.Now.Add(_delay);

                lock (_gate)
                {
                    _completeAt = next;
                    _hasSourceCompleted = true;
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
                var hasValue = false;
                var value = default(T);
                lock (_gate)
                {
                    if (_isStopped)
                    {
                        return;
                    }
                    if (_queue.Count > 0 && _queue.Peek().Timestamp <= _parent._scheduler.Now)
                    {
                        hasValue = true;
                        value = _queue.Dequeue().Value;
                        _isRunningOnNext = true;
                    }
                }

                if (hasValue)
                {
                    ForwardOnNext(value);
                    lock (_gate)
                    {
                        _isRunningOnNext = false;
                    }

                    if (_errorOccuredWhileRunningOnNext)
                    {
                        ForwardOnErrorAndStop(_error);
                    }
                    return;
                }

                if (_hasSourceCompleted && _completeAt <= _parent._scheduler.Now)
                {
                    ForwardOnCompleteAndStop();
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

            void ForwardOnCompleteAndStop() {
                _timerSubscription.Dispose();
                lock (_gate)
                {
                    _queue.Clear();
                    _isStopped = true;
                }
                try {
                    observer.OnCompleted();
                }
                finally {
                    Dispose();
                }
            }

            void ForwardOnErrorAndStop(Exception error) {
                _timerSubscription.Dispose();
                lock (_gate)
                {
                    _queue.Clear();
                    _isStopped = true;
                }
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
