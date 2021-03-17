using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aura
{
	internal class AsyncEventManager<T>
		where T : Delegate
	{
		public void Subscribe (T listener)
		{
			lock (this.listeners)
				this.listeners.Add ((listener, SynchronizationContext.Current));
		}

		public void Unsubscribe (T listener)
		{
			lock (this.listeners) {
				var item = this.listeners.FirstOrDefault (t => t.Item1 == listener);
				if (item != default)
					this.listeners.Remove (item);
			}
		}

		public void Invoke (params object[] args)
		{
			lock (this.listeners) {
				foreach (var t in this.listeners) {
					if (t.Item2 != null)
						t.Item2.Post (s => t.Item1.DynamicInvoke ((object[])s), args);
					else
						t.Item1.DynamicInvoke (args);
				}
			}
		}

		private readonly List<(Delegate, SynchronizationContext)> listeners = new List<(Delegate, SynchronizationContext)> ();
	}
}
