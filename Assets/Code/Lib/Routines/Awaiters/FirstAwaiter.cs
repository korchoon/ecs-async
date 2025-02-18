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
    public class FirstAwaiter : IRoutine<int>, ICriticalNotifyCompletion {
        public IReadOnlyList<IRoutine> Args;
        Action _continuation;
        bool _has;
        int _index;

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

            var any = false;
            for (var index = 0; index < Args.Count; index++) {
                var u = Args[index];
                u.Tick ();
                if (!_has && u.IsCompleted) {
                    _has = true;
                    _index = index;
                }

                any |= u.IsCompleted;
            }

            if (any) {
                this.DisposeAndUpdateParent ();
            }
        }

        #region async

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public FirstAwaiter GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public int GetResult () {
            Asr.IsTrue (_has);
            return _index;
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

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        #endregion
    }

    public class FirstAwaiter<T> : IRoutine<T>, ICriticalNotifyCompletion {
        public IReadOnlyList<IRoutine<T>> Args;
        Action _continuation;
        T _result;
        bool _has;

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

            var any = false;
            for (var index = 0; index < Args.Count; index++) {
                var u = Args[index];
                u.Tick ();
                if (!_has && u.IsCompleted) {
                    _has = true;
                    _result = u.GetResult ();
                }

                any |= u.IsCompleted;
            }

            if (any) {
                this.DisposeAndUpdateParent ();
            }
        }

        #region async

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public FirstAwaiter<T> GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public T GetResult () {
            Asr.IsTrue (_has);
            return _result;
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

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        #endregion
    }
#line default
}