using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aura
{
	internal abstract class NotifyingObject
		: INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged
		{
			add => this.inpcManager.Subscribe (value);
			remove => this.inpcManager.Unsubscribe (value);
		}

		protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			this.inpcManager.Invoke (this, new PropertyChangedEventArgs (propertyName));
		}

		private AsyncEventManager<PropertyChangedEventHandler> inpcManager = new AsyncEventManager<PropertyChangedEventHandler> ();
	}
}
