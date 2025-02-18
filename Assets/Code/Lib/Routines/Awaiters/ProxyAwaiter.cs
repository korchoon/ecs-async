// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mk.Scopes;

namespace Mk.Routines {
#line hidden
    public class ProxyAwaiter : IRoutine, ICriticalNotifyCompletion {
        public Func<bool> DoWhile;
        public Action<ISafeScope> OnStart;
        public Action OnBreak;
        public Action BeforeUpdate;
        public Action AfterUpdate;
        public Action BeforeDispose;
        internal IRoutine Main;
        bool _started;
        Scope _scope;

        void IDisposable.Dispose () {
            IsCompleted = true;
            BeforeDispose?.Invoke ();
            Main.Dispose ();
            _scope?.Dispose ();
        }

        void IRoutine.UpdateParent () {
            Main.UpdateParent ();
            if (Utils.TrySetNull (ref _continuation, out var c)) {
                c.Invoke ();
            }
        }

        void IRoutine.Tick () {
            if (IsCompleted) {
                return;
            }

            if (DoWhile != null && !DoWhile.Invoke ()) {
                OnBreak?.Invoke ();
                this.DisposeAndUpdateParent ();
                return;
            }

            if (!_started) {
                _started = true;
                if (OnStart != null) {
                    _scope = new ();
                    _scope.Add (Main.Dispose);
                    OnStart.Invoke (_scope);
                }
            }


            BeforeUpdate?.Invoke ();
            Main.Tick ();
            if (Main.IsCompleted) {
                this.DisposeAndUpdateParent ();
                return;
            }

            // todo bug: afterUpdate got called after disposal
            AfterUpdate?.Invoke ();
        }

        #region async

        CalledOnceGuard _guard;
        Action _continuation;

        public ProxyAwaiter GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        public void GetResult () { }

        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
            // Main.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        public bool IsCompleted { get; private set; }

        #endregion
    }

    public class ProxyAwaiter<T> : IRoutine<T>, ICriticalNotifyCompletion {
        public Action BeforeUpdateAction;
        public Action BeforeDisposeAction;
        public Action AfterUpdateAction;
        public IRoutine<T> MainRoutine;

        Action _continuation;

        public void Dispose () {
            IsCompleted = true;
            BeforeDisposeAction?.Invoke ();
            MainRoutine.Dispose ();
        }

        public void UpdateParent () {
            MainRoutine.UpdateParent ();
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick () {
            if (IsCompleted) return;

            BeforeUpdateAction?.Invoke ();
            MainRoutine.Tick ();
            if (MainRoutine.IsCompleted) {
                this.DisposeAndUpdateParent ();
                return;
            }

            AfterUpdateAction?.Invoke ();
        }

        #region async

        public ProxyAwaiter<T> GetAwaiter () => this;

        public T GetResult () {
            return MainRoutine.GetResult ();
        }

        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
            // MainRoutine.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        public bool IsCompleted { get; private set; }

        #endregion
    }
#line default
}