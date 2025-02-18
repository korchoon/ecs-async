// ----------------------------------------------------------------------------
// The MIT License
// Rollback https://github.com/korchoon/rollback
// Copyright (c) 2016-2025 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------
using System;

namespace Mk.Scopes
{
	/// <summary>
	/// A container for deferred actions executed upon Dispose, designed for resource cleanup or canceling side effects.
	/// To allow disposal, pass the Rollback.
	/// To prevent disposal, pass the IRollback.
	/// </summary>
	public sealed class Scope : ISafeScope, IDisposable
	{
		/// <summary>
		/// Tells if the rollback is being disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }
		private readonly object lockObject;
		private Action deferredActions;

		public Scope()
		{
			lockObject = new object();
			IsDisposed = false;
		}

		/// <summary>
		/// Adds an action to be executed automatically when the Dispose method is invoked.
		/// Actions will be executed in the reverse order of their addition.
		/// </summary>
		/// <param name="action">The action to be executed upon disposal.</param>
		public void Add(Action action)
		{
			lock (lockObject)
			{
				if (IsDisposed)
				{
#if DEBUG
					throw new Exception("The rollback is disposed. Cannot defer action");
#else
					return;
#endif
				}

				deferredActions = action + deferredActions; // First In Last Out order
			}
		}

		/// <summary>
		/// Removes a previously added action to prevent its execution upon disposal.
		/// The provided action must be the same delegate instance as the one initially added.
		/// </summary>
		/// <param name="action">The action delegate to be removed.</param>
		public void Remove(Action action)
		{
			lock (lockObject)
			{
				if (IsDisposed)
				{
#if DEBUG
					throw new Exception("The rollback is disposed. Cannot remove deferred action.");
#else
					return;
#endif
				}
#if DEBUG
				var countPrev = Length();
				deferredActions -= action;
				if (Length() == countPrev)
				{
					throw new Exception("Trying to remove action which wasn't deferred");
				}

				int Length()
				{
					if (deferredActions == null) return 0;
					return deferredActions.GetInvocationList().Length;
				}
#else
			deferredActions -= action;
#endif
			}
		}


		/// <summary>
		/// Executes deferred actions in the reverse order in which they were added
		/// </summary>
		public void Dispose()
		{
			lock (lockObject)
			{
				if (IsDisposed)
				{
					return;
				}

				IsDisposed = true;
			}

			if (deferredActions == null)
			{
				return;
			}

			foreach (var a in deferredActions.GetInvocationList())
			{
				var action = (Action)a;
				try
				{
					action();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
				}
			}
			
			deferredActions = null;
		}
	}
}