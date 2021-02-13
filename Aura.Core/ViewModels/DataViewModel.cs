using System;
using System.Threading.Tasks;

namespace Aura.ViewModels
{
	internal abstract class DataViewModel
		: BusyViewModel
	{
		protected DataViewModel (IAsyncServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider ?? throw new ArgumentNullException (nameof (serviceProvider));
			SetupTask = SetupAsync ();
		}

		protected DataViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService)
		{
			ServiceProvider = serviceProvider ?? throw new ArgumentNullException (nameof (serviceProvider));
			SyncService = syncService;
			SetupTask = SetupAsync ();
		}

		protected IAsyncServiceProvider ServiceProvider
		{
			get;
		}

		protected ISyncService SyncService
		{
			get;
			private set;
		}

		protected Task SetupTask
		{
			get;
			private set;
		}

		protected virtual async Task SetupAsync ()
		{
			if (SyncService == null)
				SyncService = await ServiceProvider.GetServiceAsync<ISyncService> ();
		}
	}
}
