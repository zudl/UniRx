using System;
using System.Collections;
using System.Collections.Generic;

namespace UniRx.Operators
{
    internal class DelayFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> _source;
        readonly int _frameCount;
        readonly FrameCountType _frameCountType;

        public DelayFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            _source = source;
            _frameCount = frameCount;
            _frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DelayFrame(this, observer, cancel).Run();
        }

        class DelayFrame : OperatorObserverBase<T, T>
        {
            readonly DelayFrameObservable<T> _parent;
            readonly object _gate = new object();
            readonly Queue<Timestamped> _queue = new Queue<Timestamped>();
            IDisposable _sourceSubscription;
            BooleanDisposable _tickCancellation;
            int _tickCount;
            bool _isRunning;
            bool _hasSourceCompleted;
            int _completeOnTick;
            bool _errorOccuredWhileRunning;
            Exception _error;

            public DelayFrame(DelayFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                _tickCancellation = new BooleanDisposable();
                StartDrainCoroutine(_tickCancellation);

                var sourceSubscription = new SingleAssignmentDisposable();
                _sourceSubscription = sourceSubscription;
                sourceSubscription.Disposable = _parent._source.Subscribe(this);

                return StableCompositeDisposable.Create(_tickCancellation, _sourceSubscription);
            }

            void StartDrainCoroutine(BooleanDisposable cancellationToken) {
                switch (_parent._frameCountType) {
                    case FrameCountType.Update:
                        MainThreadDispatcher.StartUpdateMicroCoroutine(DrainCoroutine(cancellationToken));
                        break;
                    case FrameCountType.FixedUpdate:
                        MainThreadDispatcher.StartFixedUpdateMicroCoroutine(DrainCoroutine(cancellationToken));
                        break;
                    case FrameCountType.EndOfFrame:
                        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(DrainCoroutine(cancellationToken));
                        break;
                    default:
                        throw new ArgumentException("Invalid FrameCountType:" + _parent._frameCountType);
                }
            }

            public override void OnNext(T value)
            {
                if (_tickCancellation.IsDisposed) return;

                lock (_gate)
                {
                    _queue.Enqueue(new Timestamped(value, _tickCount + _parent._frameCount + 1));
                }
            }

            public override void OnError(Exception error)
            {
                _sourceSubscription.Dispose(); // stop subscription

                if (_tickCancellation.IsDisposed) return;

                lock (_gate)
                {
                    if (_isRunning)
                    {
                        _error = error;
                        _errorOccuredWhileRunning = true;
                        return;
                    }
                }

                _tickCancellation.Dispose();
                ForwardOnError(error);
            }

            public override void OnCompleted()
            {
                _sourceSubscription.Dispose(); // stop subscription

                if (_tickCancellation.IsDisposed) return;

                lock (_gate)
                {
                    _completeOnTick = _tickCount + _parent._frameCount + 1;
                    _hasSourceCompleted = true;
                }
            }

            void ForwardOnNext(T nextValue) {
                observer.OnNext(nextValue);
            }

            void ForwardOnComplete() {
                try {
                    observer.OnCompleted();
                }
                finally {
                    Dispose();
                }
            }

            void ForwardOnError(Exception error) {
                try {
                    observer.OnError(error);
                }
                finally {
                    Dispose();
                }
            }

            IEnumerator DrainCoroutine(BooleanDisposable cancellationToken)
            {
                while (!cancellationToken.IsDisposed)
                {
                    _tickCount++;
                    var hasValue = false;
                    var nextValue = default(T);

                    // Emit all values delayed till current frame one by one
                    do
                    {
                        lock (_gate)
                        {
                            if (_queue.Count > 0 && _queue.Peek().Tick <= _tickCount)
                            {
                                hasValue = true;
                                nextValue = _queue.Dequeue().Value;
                            }
                            else
                            {
                                hasValue = false;
                            }
                        }

                        if (cancellationToken.IsDisposed)
                        {
                            yield break;
                        }

                        if (hasValue)
                        {
                            lock (_gate)
                            {
                                _isRunning = true;
                            }

                            ForwardOnNext(nextValue);

                            lock (_gate)
                            {
                                _isRunning = false;
                            }

                            if (_errorOccuredWhileRunning)
                            {
                                cancellationToken.Dispose();
                                ForwardOnError(_error);
                                yield break;
                            }
                        }

                        if (!hasValue && _hasSourceCompleted && _completeOnTick <= _tickCount)
                        {
                            cancellationToken.Dispose();
                            ForwardOnComplete();
                            yield break;
                        }
                    } while (hasValue);

                    yield return null;
                }
            }
        }

        struct Timestamped
        {
            public readonly T Value;
            public readonly int Tick;

            public Timestamped(T value, int tick) : this()
            {
                Value = value;
                Tick = tick;
            }
        }
    }
}
