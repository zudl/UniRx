using System.Collections;
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
    }
}
