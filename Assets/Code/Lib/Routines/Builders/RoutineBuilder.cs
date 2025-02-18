// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security;
using JetBrains.Annotations;
using Mk;
using Mk.Routines;
using Mk.Scopes;
using UnityEngine;


namespace Mk.Routines {
#line hidden

    public class RoutineBuilder {
        static DebugFileInfo _lineCache = new ();
        Action _continuation;
        Scope _taskScope;

        [UsedImplicitly]
        public Routine Task { get; }

        RoutineBuilder (Routine r) {
            // Dbg.Line ();
            Task = r;
        }

        [UsedImplicitly]
        public static RoutineBuilder Create () {
            var task = new Routine ();
            var res = new RoutineBuilder (task);
            return res;
        }

        [UsedImplicitly]
        public void Start<TStateMachine> (ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine {
            _continuation = stateMachine.MoveNext;
            Task.Start = _continuation;
            // _continuation.Invoke();
        }


        [UsedImplicitly]
        public void AwaitOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion, IRoutine
            where TStateMachine : IAsyncStateMachine {
            switch (awaiter) {
                case SelfParallelAwaiter attachedRoutinesAwaiter:
                    if (!Task.AttachedRoutines.TryGet (out var value)) {
                        value = new ();
                        Task.AttachedRoutines = value;
                    }

                    attachedRoutinesAwaiter.Value = value;
                    stateMachine.MoveNext ();
                    attachedRoutinesAwaiter.OnCompleted (default);
                    return;
                case SelfRollbackAwaiter selfScopeAwaiter:
                    if (_taskScope == null) _taskScope = Task.TaskScope = new ();
                    Task.TaskScope.Add (Task.Dispose);
                    selfScopeAwaiter.Value = _taskScope;
                    stateMachine.MoveNext ();
                    selfScopeAwaiter.OnCompleted (default);
                    return;
            }

            awaiter.OnCompleted (_continuation);
            Task.CurrentAwaiter = awaiter;
            awaiter.Tick ();
#if MK_TRACE
            _lineCache.SetDebugName (ref Task.__Await, 2);
#endif
        }

        [UsedImplicitly]
        public void SetResult () {
#if MK_TRACE
            _lineCache.SetDebugName (ref Task.__Await, 1);
            Task.__Await = $"[Returned at] {Task.__Await}";
#endif
            Task.DisposeAndUpdateParent ();
        }

#line hidden
        [UsedImplicitly]
        public void SetException (Exception e) {
#if MK_TRACE
            _lineCache.SetDebugName (ref Task.__Await, 1);
            Task.__Await = $"[Exception at] {e.Message} {Task.__Await}";
#endif
            Debug.LogException (e);
            Task.Dispose ();
            // Task.BreakAndUpdateParent ();
        }
#line default

        [UsedImplicitly]
        [SecuritySafeCritical,]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion, IRoutine
            where TStateMachine : IAsyncStateMachine {
            AwaitOnCompleted (ref awaiter, ref stateMachine);
        }

        [UsedImplicitly]
        public void SetStateMachine (IAsyncStateMachine stateMachine) {
            _continuation = stateMachine.MoveNext;
        }
    }
#line default
}