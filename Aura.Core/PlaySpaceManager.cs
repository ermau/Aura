using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Data;
using Aura.Messages;

namespace Aura
{
	internal class PlaySpaceManager
		: SingleSelectionManager<PlaySpaceElement>
	{
		public PlaySpaceManager (ISyncService syncService, ISettingsManager settings)
			: base (syncService)
		{
			Messenger.Default.Register<EnableServiceMessage> (this, OnEnableService);
			this.settings = settings ?? throw new ArgumentNullException (nameof (settings));
		}

		/// <summary>
		/// Gets the enabled services for the current play space out of the <paramref name="availableServices"/>.
		/// </summary>
		public IEnumerable<IEnvironmentService> GetServices (IEnumerable<IEnvironmentService> availableServices)
		{
			if (availableServices is null)
				throw new ArgumentNullException (nameof (availableServices));

			PlaySpaceElement space = SelectedElement;
			return availableServices.Where (s => space.Services.Contains (s.GetType().GetSimpleTypeName()));
		}

		public async Task EnableServiceAsync (IEnvironmentService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			PlaySpaceElement space = SelectedElement;
			var services = new List<string> (space.Services.Count + 1);
			services.AddRange (space.Services);
			services.Add (service.GetType ().GetSimpleTypeName());

			space = space with { Services = services };
			await SyncService.SaveElementAsync (space);
			await Loading;
		}

		public async Task<bool> GetIsServiceEnabledAsync (IService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			await Loading;
			var e = SelectedElement;
			return (e != null && e.Services.Contains (service.GetType ().GetSimpleTypeName ()));
		}

		private ISettingsManager settings;

		private async void OnEnableService (EnableServiceMessage msg)
		{
			if (msg.Service is IEnvironmentService environmentService)
				await EnableServiceAsync (environmentService);
		}
	}
}