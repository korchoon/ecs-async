// ----------------------------------------------------------------------------
// The MIT License
// scope https://github.com/korchoon/scope
// Copyright (c) 2016-2025 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mk.Scopes {
    public static class ScopeExtensions {
        /// <summary>
        /// Throws a TaskCanceledException if the scope instance has been disposed. 
        /// This method is intended for use within <see cref="System.Threading.Tasks.Task"/> or <see cref="async Task"/>  to ensure 
        /// proper handling of disposal scenarios, similar to <see cref="System.Threading.CancellationToken.ThrowIfCancellationRequested"/>.
        /// </summary>
        /// <param name="scope">The scope instance to check for disposal.</param>
        /// <exception cref="TaskCanceledException">Thrown if the scope instance has been disposed.</exception>
        /// <example>
        /// <code>
        /// public async Task PerformActionAsync(Iscope scope)
        /// {
        ///     scope.ThrowIfDisposed();
        ///     await SomeAsyncOperation();
        /// }
        /// </code>
        /// </example>
        public static void ThrowIfDisposed (this ISafeScope scope) {
            if (scope.IsDisposed) {
                throw new TaskCanceledException ("Cancellation from scope.ThrowIfDisposed()");
            }
        }

        /// <summary>
        /// Creates a child CancellationTokenSource associated with the given scope. 
        /// Disposing the CancellationTokenSource will not dispose the scope instance.
        /// </summary>
        /// <param name="scope">The parent scope instance.</param>
        /// <returns>A CancellationTokenSource linked to the provided scope.</returns>
        public static CancellationTokenSource GetCancellationTokenSource (this ISafeScope scope) {
            if (scope.IsDisposed) {
                throw new Exception ("Cannot get CancellationToken from scope that is disposed");
            }

            var source = new CancellationTokenSource ();
            scope.Add (source.Dispose);
            scope.Add (source.Cancel);
            return source;
        }

        /// <summary>
        /// Cancellation token associated with scope
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static CancellationToken GetCancellationToken (this ISafeScope scope) {
            var source = scope.GetCancellationTokenSource ();
            return source.Token;
        }

        /// <summary>
        /// Opens a child scope instance tied to the current one, enabling cascading disposal.
        /// Disposing the child does not dispose the parent, but disposing the parent will dispose both the parent and the child.
        /// </summary>
        /// <returns>A new child scope instance associated with the current parent scope.</returns>
        public static Scope NestedScope (this ISafeScope parentScope) {
            if (parentScope.IsDisposed) {
                throw new Exception ("Cannot open a child scope because the parent scope instance has already been disposed.");
            }

            var result = new Scope ();
            parentScope.Add (DisposeNested);
            result.Add (CancelDisposeNested);
            return result;

            void DisposeNested () {
                result.Dispose ();
            }

            void CancelDisposeNested () {
                if (!parentScope.IsDisposed) {
                    parentScope.Remove (DisposeNested);
                }
            }
        }
    }
}