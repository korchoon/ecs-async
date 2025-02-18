// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System.Diagnostics;

namespace Mk.Routines {
    static class CalledOnceGuardApi {
        [Conditional (FLAGS.DEBUG)]
        public static void Assert (this ref CalledOnceGuard t) {
            UnityEngine.Assertions.Assert.IsFalse (t.Called, "Awaiter should be called once");
            t.Called = true;
        }
    }

    struct CalledOnceGuard {
        public bool Called;
    }
}