// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Mk.Routines {
#line hidden
    public class FuncAwaiter<T> : IRoutine<T>, ICriticalNotifyCompletion where T : IOption {
        public Func<T> TryGet;
        public Action OnDispose;
        Action _continuation;
        T _cached;

        public void Dispose () {
            if (IsCompleted) return;
            IsCompleted = true;
            OnDispose?.Invoke ();
        }

        void IRoutine.UpdateParent () {
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick () {
            if (IsCompleted) return;

            _cached = TryGet.Invoke ();
            if (_cached.HasValue) {
                this.DisposeAndUpdateParent ();
            }
        }

        #region async

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public FuncAwaiter<T> GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        [UsedImplicitly]
        public T GetResult () => _cached;

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
#line default
}