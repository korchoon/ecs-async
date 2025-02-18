// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Mk.Routines {
#line hidden
    public class AllAwaiter : IRoutine, ICriticalNotifyCompletion {
        public IReadOnlyList<IRoutine> Args;
        Action _continuation;

        public void Dispose () {
            if (IsCompleted) return;
            IsCompleted = true;
            foreach (var u in Args) u.Dispose ();
        }

        public void UpdateParent () {
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick () {
            if (IsCompleted) return;

            var all = true;
            for (var index = 0; index < Args.Count; index++) {
                var u = Args[index];
                u.Tick ();
                all &= u.IsCompleted;
            }

            if (all) {
                this.DisposeAndUpdateParent ();
            }
        }

        #region async

        [UsedImplicitly]
        public AllAwaiter GetAwaiter () {
            return this;
        }

        [UsedImplicitly]
        public void GetResult () { }

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
        public void UnsafeOnCompleted (Action continuation) {
            OnCompleted (continuation);
        }

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        #endregion
    }
#line default
#line hidden
    public class AllAwaiter<T> : IRoutine<IReadOnlyList<IRoutine<T>>>, ICriticalNotifyCompletion {
        public IReadOnlyList<IRoutine<T>> Args;
        
        Action _continuation;

        public void Dispose () {
            if (IsCompleted) return;
            IsCompleted = true;
            foreach (var u in Args) u.Dispose ();
        }

        public void UpdateParent () {
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick () {
            if (IsCompleted) return;

            var all = true;
            for (var index = 0; index < Args.Count; index++) {
                var u = Args[index];
                u.Tick ();
                all &= u.IsCompleted;
            }

            if (all) {
                this.DisposeAndUpdateParent ();
            }
        }

        #region async

        [UsedImplicitly]
        public AllAwaiter<T> GetAwaiter () {
            return this;
        }

        [UsedImplicitly]
        public IReadOnlyList<IRoutine<T>> GetResult()
        {
	        return Args;
	        
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
        public void UnsafeOnCompleted (Action continuation) {
            OnCompleted (continuation);
        }

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        #endregion
    }
#line default
}