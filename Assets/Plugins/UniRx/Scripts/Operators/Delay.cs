using System;
using System.Collections.Generic;

namespace UniRx.Operators
{
    internal class DelayObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> _source;
        readonly TimeSpan _dueTime;
        readonly IScheduler _scheduler;

        public DelayObservable(IObservable<T> source, TimeSpan dueTime, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this._source = source;
            this._dueTime = dueTime;
            this._scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Delay(this, observer, cancel).Run();
        }

        class Delay : OperatorObserverBase<T, T>
        {
            readonly DelayObservable<T> _parent;
            readonly object _gate = new object();
            bool _hasFailed;
            bool _running;
            bool _active;
            Exception _exception;
            Queue<Timestamped<T>> _queue;
            bool _hasCompleted;
            DateTimeOffset _completeAt;
            IDisposable _sourceSubscription;
            TimeSpan _delay;
            bool _ready;
            SerialDisposable _cancelable;

            public Delay(DelayObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this._parent = parent;
            }

            public IDisposable Run()
            {
                _cancelable = new SerialDisposable();

                _active = false;
                _running = false;
                _queue = new Queue<Timestamped<T>>();
                _hasCompleted = false;
                _completeAt = default(DateTimeOffset);
                _hasFailed = false;
                _exception = default(Exception);
                _ready = true;
                _delay = Scheduler.Normalize(_parent._dueTime);

                var sourceSubscription = new SingleAssignmentDisposable();
                _sourceSubscription = sourceSubscription;
                sourceSubscription.Disposable = _parent._source.Subscribe(this);

                return StableCompositeDisposable.Create(_sourceSubscription, _cancelable);
            }

            public override void OnNext(T value)
            {
                var next = _parent._scheduler.Now.Add(_delay);
                var shouldRun = false;

                lock (_gate)
                {
                    _queue.Enqueue(new Timestamped<T>(value, next));

                    shouldRun = _ready && !_active;
                    _active = true;
                }

                if (shouldRun)
                {
                    _cancelable.Disposable = _parent._scheduler.Schedule(_delay, DrainQueue);
                }
            }

            public override void OnError(Exception error)
            {
                _sourceSubscription.Dispose();

                var shouldRun = false;

                lock (_gate)
                {
                    _queue.Clear();

                    _exception = error;
                    _hasFailed = true;

                    shouldRun = !_running;
                }

                if (shouldRun)
                {
                    try { base.observer.OnError(error); } finally { Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                _sourceSubscription.Dispose();

                var next = _parent._scheduler.Now.Add(_delay);
                var shouldRun = false;

                lock (_gate)
                {
                    _completeAt = next;
                    _hasCompleted = true;

                    shouldRun = _ready && !_active;
                    _active = true;
                }

                if (shouldRun)
                {
                    _cancelable.Disposable = _parent._scheduler.Schedule(_delay, DrainQueue);
                }
            }

            void DrainQueue(Action<TimeSpan> recurse)
            {
                lock (_gate)
                {
                    if (_hasFailed) return;
                    _running = true;
                }

                var shouldYield = false;

                while (true)
                {
                    var hasFailed = false;
                    var error = default(Exception);

                    var hasValue = false;
                    var value = default(T);
                    var hasCompleted = false;

                    var shouldRecurse = false;
                    var recurseDueTime = default(TimeSpan);

                    lock (_gate)
                    {
                        if (_hasFailed)
                        {
                            error = _exception;
                            hasFailed = true;
                            _running = false;
                        }
                        else
                        {
                            if (_queue.Count > 0)
                            {
                                var nextDue = _queue.Peek().Timestamp;

                                if (nextDue.CompareTo(_parent._scheduler.Now) <= 0 && !shouldYield)
                                {
                                    value = _queue.Dequeue().Value;
                                    hasValue = true;
                                }
                                else
                                {
                                    shouldRecurse = true;
                                    recurseDueTime = Scheduler.Normalize(nextDue.Subtract(_parent._scheduler.Now));
                                    _running = false;
                                }
                            }
                            else if (_hasCompleted)
                            {
                                if (_completeAt.CompareTo(_parent._scheduler.Now) <= 0 && !shouldYield)
                                {
                                    hasCompleted = true;
                                }
                                else
                                {
                                    shouldRecurse = true;
                                    recurseDueTime = Scheduler.Normalize(_completeAt.Subtract(_parent._scheduler.Now));
                                    _running = false;
                                }
                            }
                            else
                            {
                                _running = false;
                                _active = false;
                            }
                        }
                    }

                    if (hasValue)
                    {
                        base.observer.OnNext(value);
                        shouldYield = true;
                    }
                    else
                    {
                        if (hasCompleted)
                        {
                            try { base.observer.OnCompleted(); } finally { Dispose(); }
                        }
                        else if (hasFailed)
                        {
                            try { base.observer.OnError(error); } finally { Dispose(); }
                        }
                        else if (shouldRecurse)
                        {
                            recurse(recurseDueTime);
                        }

                        return;
                    }
                }
            }
        }
    }
}
