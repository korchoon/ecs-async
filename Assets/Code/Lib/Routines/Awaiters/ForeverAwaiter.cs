// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Mk.Routines {
    public class ForeverAwaiter : IRoutine, ICriticalNotifyCompletion {
        public static ForeverAwaiter Instance { get; } = new ();

        ForeverAwaiter() { }

        public void UpdateParent() { }
        public void Tick() { }
        public void Dispose() { }

        #region async

        [UsedImplicitly]
        public bool IsCompleted { get; } = false;

        [UsedImplicitly]
        public void GetResult() { }

        [UsedImplicitly]
        public void OnCompleted(Action _) { }

        [UsedImplicitly]
        public void UnsafeOnCompleted(Action _) { }

        [UsedImplicitly]
        public ForeverAwaiter GetAwaiter() => this;

        #endregion
    }
}