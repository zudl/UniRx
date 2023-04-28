using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniRx.Tests
{
    public class ObservableFrameTest
    {
        [UnityTest]
        public IEnumerator ThrottleFrameTest()
        {
            yield return null;

            // When a framecount-based operator is triggered in Update phase:
            // - with FrameCountType.Update the first tick happens on the next frame
            // - with FrameCountType.EndOfFrame the first tick happens on the current frame
            Notification<int>[] emittedValues = null;
            yield return Observable.Concat(
                    Observable.Return(1).DelayFrame(0), // next delay is 1 skipped frame + 1 delayed frame < 4 frames + eof phase
                    Observable.Return(2).DelayFrame(1), // next delay is 1 skipped frame + 2 delayed frames < 4 frames + eof phase
                    Observable.Return(3).DelayFrame(2), // next delay is 1 skipped frame + 3 delayed frames < 4 frames + eof phase
                    Observable.Return(4).DelayFrame(3), // next delay is 1 skipped frame + 4 delayed frames > 4 frames + eof phase, emit 4
                    Observable.Return(5).DelayFrame(4)  // last value, emit 5
                )
                .ThrottleFrame(4, FrameCountType.EndOfFrame)
                .Materialize()
                .ToArray()
                .Do(array => emittedValues = array)
                .ToAwaitableEnumerator();

            emittedValues.Length.Is(3);
            emittedValues[0].Value.Is(4);
            emittedValues[1].Value.Is(5);
            emittedValues[2].Kind.Is(NotificationKind.OnCompleted);
        }

        [UnityTest]
        public IEnumerator ThrottleFirstFrameTest()
        {
            yield return null;

            // When a framecount-based operator is triggered in Update phase:
            // - with FrameCountType.Update the first tick happens on the next frame
            // - with FrameCountType.EndOfFrame the first tick happens on the current frame
            Notification<int>[] emittedValues = null;
            yield return Observable.Concat(
                    Observable.Return(1).DelayFrame(0), // first value, emit 1
                    Observable.Return(2).DelayFrame(1), // total delay is 1 skipped frame + 1 delayed frame < 5 frames + eof phase
                    Observable.Return(3).DelayFrame(2), // total delay is 2 skipped frames + 3 delayed frames < 5 frames + eof phase
                    Observable.Return(4).DelayFrame(0), // total delay is 3 skipped frames + 3 delayed frames > 5 frames + eof phase, emit 4
                    Observable.Return(5).DelayFrame(4)  // total delay is 1 skipped frame + 4 delayed frames < 5 frames + eof phase
                )
                .ThrottleFirstFrame(5, FrameCountType.EndOfFrame)
                .Materialize()
                .ToArray()
                .Do(array => emittedValues = array)
                .ToAwaitableEnumerator();

            emittedValues.Length.Is(3);
            emittedValues[0].Value.Is(1);
            emittedValues[1].Value.Is(4);
            emittedValues[2].Kind.Is(NotificationKind.OnCompleted);
        }

        [UnityTest]
        public IEnumerator DelayFrameCompleteTest()
        {
            yield return null;

            const int Delay = 5;
            ValueTuple<Notification<int>, int>[] result = null;

            yield return Observable.Concat(
                    Observable.Repeat(Unit.Default, 3), // emit 3 values on frame 0
                    Observable.EveryUpdate() // skip 1 frame to sync with Unity loop and 1 waiting for next Update
                        .Take(9)
                        .Where(index => index == 0  // emit 1 value on frame 2
                                        || index == 4  // emit 1 value on frame 6
                                        || index == 5) // emit 1 value on frame 7
                        .AsUnitObservable() // complete on frame 10
                )
                .Select(_ => Time.frameCount)
                .DelayFrame(Delay, FrameCountType.EndOfFrame)
                .Materialize()
                .Select(n => new ValueTuple<Notification<int>, int>(n, Time.frameCount))
                .ToArray()
                .Do(array => result = array)
                .ToYieldInstruction();

            result.Length.Is(7);

            for (var i = 0; i < 6; i++)
            {
                result[i].Item1.Kind.Is(NotificationKind.OnNext);
                result[i].Item2.Is(result[i].Item1.Value + Delay);
            }
            result[6].Item1.Kind.Is(NotificationKind.OnCompleted);
            result[6].Item2.Is(result[0].Item2 + 10);
        }

        [UnityTest]
        public IEnumerator DelayFrameErrorTest()
        {
            yield return null;

            const int Delay = 5;
            ValueTuple<Notification<int>, int>[] result = null;

            yield return Observable.Concat(
                    Observable.EveryUpdate().Take(10).AsUnitObservable(), // emit 1 value each frame during 10 frames
                    Observable.Throw<Unit>(new Exception()) // throw on frame 10
                )
                .Select(_ => Time.frameCount)
                .DelayFrame(Delay, FrameCountType.EndOfFrame)
                .Materialize()
                .Select(n => new ValueTuple<Notification<int>, int>(n, Time.frameCount))
                .ToArray()
                .Do(array => result = array)
                .ToYieldInstruction();

            result.Length.Is(5);

            for (var i = 0; i < 4; i++)
            {
                result[i].Item1.Kind.Is(NotificationKind.OnNext);
                result[i].Item2.Is(result[i].Item1.Value + Delay);
            }
            result[4].Item1.Kind.Is(NotificationKind.OnError);
            result[4].Item2.Is(result[3].Item2 + 1);
        }
    }
}
