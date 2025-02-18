// ----------------------------------------------------------------------------
// The MIT License
// Rollback https://github.com/korchoon/rollback
// Copyright (c) 2016-2025 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Mk.Scopes {
    /// <summary>
    /// A container for actions deferred until Dispose, used for resource cleanup or canceling side effects
    /// </summary>
    public interface ISafeScope {
        /// <summary>
        /// Tells if the scope is being disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Adds an action to be executed automatically when the Dispose method is invoked.
        /// Actions will be executed in the reverse order of their addition.
        /// </summary>
        /// <param name="action">The action to be executed upon disposal.</param>
        void Add (Action action);

        /// <summary>
        /// Removes a previously added action to prevent its execution upon disposal.
        /// The provided action must be the same delegate instance as the one initially added.
        /// </summary>
        /// <param name="action">The action delegate to be removed.</param>
        void Remove (Action action);
    }
}