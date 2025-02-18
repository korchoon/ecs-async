// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mk.Scopes;

namespace Mk.Routines {
#line hidden

    [PublicAPI]
#if MK_TRACE
    [Serializable]
#endif
    [AsyncMethodBuilder (typeof (RoutineBuilder))]
    public sealed class Routine : IRoutine, ICriticalNotifyCompletion {
        internal Action Start;
        public string __Await;
        internal Option<AttachedRoutines> AttachedRoutines;
        internal Scope TaskScope;
        internal IRoutine CurrentAwaiter;

        Action _continuation;

        internal Routine () { }

        public void Dispose () {
            if (IsCompleted) return;

            Start = null;
            IsCompleted = true;
            if (AttachedRoutines.TryGet (out var attachedRoutines)) {
                AttachedRoutines = default;
                for (var i = attachedRoutines.Routines.Count - 1; i >= 0; i--)
                    attachedRoutines.Routines[i].Dispose ();
            }

            if (Utils.TrySetNull (ref CurrentAwaiter, out var buf)) buf.Dispose ();
            TaskScope?.Dispose ();
#if MK_TRACE
            __Await = $"[Interrupted at] {__Await}";
#endif
        }

        void IRoutine.UpdateParent () {
            if (AttachedRoutines.TryGet (out var attachedRoutines))
                for (var i = 0; i < attachedRoutines.Routines.Count; i++)
                    attachedRoutines.Routines[i].UpdateParent ();

            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Tick () {
            if (IsCompleted) return;

            if (Utils.TrySetNull (ref Start, out var start)) {
                start.Invoke ();
                return;
            }

            if (AttachedRoutines.TryGet (out var attachedRoutines))
                for (var i = 0; i < attachedRoutines.Routines.Count; i++) {
                    if (IsCompleted) return;
                    attachedRoutines.Routines[i].Tick ();
                }

            CurrentAwaiter?.Tick ();
        }

        #region async

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public Routine GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public void GetResult () {
            // Asr.IsFalse(Interrupted);
        }

        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
        }

        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion

        #region static api

        static InfLoopSafety _safety = new (10000);

        public static async Routine While (Func<bool> enterIf, Action<ISafeScope> start = null, Action update = null, Func<IRoutine> routine = default) {
            _safety.Check ();
            if (!enterIf.Invoke ()) {
#if PREVENT_INF_LOOP
                 await Routine.Yield;
#endif
                return;
            }

            if (routine != null) {
                await routine.Invoke ().Configure (doWhile: enterIf, onStart: start, afterUpdate: update);
                await When (() => !enterIf ());
            }
            else {
                var rollback = await GetScope ();
                start?.Invoke (rollback);
                await When (() => !enterIf ()).Configure (afterUpdate: update);
            }
        }

        static class WithResult<T> {
            static Routine<T> _emptyInstance;
        }

        public static TryAwaiter WaitSecondsByDeltaTime (float seconds, Func<float> getDeltaTimeSeconds) {
            var timeLeft = seconds;

            return When (() => {
                var prev = timeLeft;
                timeLeft -= getDeltaTimeSeconds ();
                return prev < 0f;
            });
        }


        static TryAwaiter WaitSecondsByTime (float seconds, Func<float> getTime) {
            var endTime = getTime () + seconds;
            return When (() => endTime < getTime ());
        }


        [MustUseReturnValue]
        public static ActionAwaiter FromActions (
            Action update,
            Action dispose = default,
            Func<bool> doWhile = default,
            Action<ISafeScope> onStart = default)
            => new () {
                OnUpdate = update,
                DoWhile = doWhile,
                BeforeDispose = dispose,
                OnStart = onStart
            };

        [MustUseReturnValue]
        public static TryAwaiter When (Func<bool> p, Action onDispose = default) => new () { TryGet = p, OnDispose = onDispose };

        [MustUseReturnValue]
        public static TryAwaiter<T> WhenGet<T> (Func<Option<T>> p) => new () { TryGet = p };

        [MustUseReturnValue]
        public static AnyAwaiter WhenAny (params IRoutine[] args) => new () { Args = args };

        [MustUseReturnValue]
        public static AnyAwaiter<T> WhenAny_T<T> (params IRoutine<T>[] args) => new () { Args = args };

        [MustUseReturnValue]
        public static AnyAwaiter WhenAny (IReadOnlyList<IRoutine> args) => new () { Args = args };

        [MustUseReturnValue]
        public static FirstAwaiter<T> WhenFirst<T> (IReadOnlyList<IRoutine<T>> args) => new () { Args = args };

        static Option<T> TryGet<T> (IRoutine<T> r) {
            if (!r.IsCompleted) {
                return default;
            }

            return r.GetResult ();
        }

        [MustUseReturnValue]
        public static FirstAwaiter<T> WhenFirst<T> (params IRoutine<T>[] args) => new () { Args = args };

        [MustUseReturnValue]
        public static FirstAwaiter WhenFirst (params IRoutine[] args) {
            return new () { Args = args };
        }

        [MustUseReturnValue]
        public static AllAwaiter WhenAll (params IRoutine[] args) => new () { Args = args };

        public static AllAwaiter<T> WhenAll<T> (params IRoutine<T>[] args) => new () { Args = args };

        [MustUseReturnValue]
        public static AllAwaiter WhenAll (IReadOnlyList<IRoutine> args) => new () { Args = args };

        [MustUseReturnValue]
        public static SelfRollbackAwaiter GetScope () => new ();

        [MustUseReturnValue]
        public static SelfParallelAwaiter GetParallel () => new ();

        public static YieldAwaiter Yield => YieldAwaiter.Pool.Pop ();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Routine<T> FromResult<T> (T res) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            return res;
        }

        public static YieldAwaiterTicks YieldTicks (int count) => new (count);

        public static ForeverAwaiter Forever { get; } = ForeverAwaiter.Instance;


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Routine Completed () { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        #endregion
    }
#line default
}