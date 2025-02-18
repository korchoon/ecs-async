// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Mk.Routines {
    public interface IRoutine : IDisposable{
        bool IsCompleted { get; }
        void Tick ();
        void UpdateParent ();
    }

    public interface IRoutine<out T> : IRoutine {
        T GetResult ();
    }
}