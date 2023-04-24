using System;

#if UniRxLibrary
using UnityObservable = UniRx.ObservableUnity;
#else
using UnityObservable = UniRx.Observable;
#endif

namespace UniRx.Operators
{
    internal class ThrottleFirstFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public ThrottleFirstFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFirstFrame(this, observer, cancel).Run();
        }

        class ThrottleFirstFrame : OperatorObserverBase<T, T>
        {
            readonly ThrottleFirstFrameObservable<T> parent;
            readonly object gate = new object();
            int framesTillOpen = -1;
            IDisposable timer;

            public ThrottleFirstFrame(ThrottleFirstFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
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
                    if (framesTillOpen > 0) return;
                    framesTillOpen = parent.frameCount + 1;
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

            IDisposable CreateTimer()
            {
                return UnityObservable.TimerFrame(0, 1, parent.frameCountType)
                    .Subscribe(new ThrottleFirstFrameTick(this));
            }

            // immutable, can share.
            class ThrottleFirstFrameTick : IObserver<long>
            {
                readonly ThrottleFirstFrame parent;

                public ThrottleFirstFrameTick(ThrottleFirstFrame parent)
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
                        if (parent.framesTillOpen > 0)
                        {
                            parent.framesTillOpen--;
                        }
                    }
                }
            }
        }
    }
}
