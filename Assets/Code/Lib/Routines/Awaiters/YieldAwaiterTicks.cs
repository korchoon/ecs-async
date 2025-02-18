using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Mk.Routines
{
    public class YieldAwaiterTicks : IRoutine, ICriticalNotifyCompletion {
        int _updatedCount;
        readonly int _targetCount;
        Action _continuation;

        public YieldAwaiterTicks(int targetCount) {
            Asr.IsTrue(targetCount > 0);
            _targetCount = targetCount;
        }

        public void Dispose () {
            if (!IsCompleted) {
                IsCompleted = true;
            }
        }
        
        public void UpdateParent () {
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick ()
        {
            _updatedCount += 1;
            if (_updatedCount <= _targetCount) {
                return;
            }

            if (IsCompleted) return;
            this.DisposeAndUpdateParent ();
        }

        #region async

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        [UsedImplicitly]
        public void GetResult () { }

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public YieldAwaiterTicks GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
        }

        [UsedImplicitly]
        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion
    }
}