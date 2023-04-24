using System;

#if UniRxLibrary
using UnityObservable = UniRx.ObservableUnity;
#else
using UnityObservable = UniRx.Observable;
#endif

namespace UniRx.Operators
{
    internal class ThrottleFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public ThrottleFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFrame(this, observer, cancel).Run();
        }

        class ThrottleFrame : OperatorObserverBase<T, T>
        {
            readonly ThrottleFrameObservable<T> parent;
            readonly object gate = new object();
            int framesSinceValueSet;
            T latestValue = default(T);
            bool hasValue = false;
            IDisposable timer;

            public ThrottleFrame(ThrottleFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                timer = CreateTimer();
                var subscription = parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(timer, subscription);
            }

            public override void OnNext(T value)
            {
                lock (gate)
                {
                    hasValue = true;
                    latestValue = value;
                    framesSinceValueSet = 0;
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

            IDisposable CreateTimer()
            {
                return UnityObservable.TimerFrame(0, 1, parent.frameCountType)
                    .Subscribe(new ThrottleFrameTick(this));
            }

            class ThrottleFrameTick : IObserver<long>
            {
                readonly ThrottleFrame parent;

                public ThrottleFrameTick(ThrottleFrame parent)
                {
                    this.parent = parent;
                }

                public void OnCompleted()
                {
                }

                public void OnError(Exception error)
                {
                }

                public void OnNext(long _)
                {
                    lock (parent.gate)
                    {
                        if (parent.hasValue && parent.framesSinceValueSet++ >= parent.parent.frameCount)
                        {
                            parent.hasValue = false;
                            parent.observer.OnNext(parent.latestValue);
                        }
                    }
                }
            }
        }
    }
}
