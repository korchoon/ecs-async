// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mk.Scopes;
using UnityEngine;

namespace Mk.Routines {
    [Serializable]
    public class AttachedRoutines {
        [SerializeReference] internal List<IRoutine> Routines = new ();

        public void AttachUpdate (Action action) {
            Attach (Routine.FromActions (action));
        }

        public void Attach (IRoutine r) {
            Routines.Add (r);
            r.Tick (); // todo ?
        }
        
        public void AttachScoped (ISafeScope scope, IRoutine r) {
            Attach (r);
            scope.Add (() => {
                r.Dispose ();
                Detach (r);
            });
        }

        public void Detach (IRoutine r) {
            Routines.Remove (r);
        }
    }

    public class SelfParallelAwaiter : IRoutine, ICriticalNotifyCompletion {
        internal AttachedRoutines Value;
        public void UpdateParent () { }
        public void Tick () { }
        public void Dispose () { }

        #region async

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public SelfParallelAwaiter GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public bool IsCompleted { [UsedImplicitly] get; private set; }

        [UsedImplicitly]
        public AttachedRoutines GetResult () => Value;

        [UsedImplicitly]
        public void OnCompleted (Action continuation) => IsCompleted = true;

        [UsedImplicitly]
        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion
    }

    public class SelfRollbackAwaiter : IRoutine, ICriticalNotifyCompletion {
        internal Scope Value;
        public void UpdateParent () { }
        public void Tick () { }
        public void Dispose () { }

        #region async

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public SelfRollbackAwaiter GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public bool IsCompleted { [UsedImplicitly] get; private set; }

        [UsedImplicitly]
        public Scope GetResult () => Value;

        [UsedImplicitly]
        public void OnCompleted (Action continuation) => IsCompleted = true;

        [UsedImplicitly]
        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion
    }
}