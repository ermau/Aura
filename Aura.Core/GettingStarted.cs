using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura
{
	public class RoleQuestionEventArgs
		: EventArgs
	{
		public Task<bool> IsGameMaster
		{
			get;
			set;
		}
	}

	public class ServiceDiscoveredEventArgs
		: EventArgs
	{
		public ServiceDiscoveredEventArgs (IDiscoverableService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			Service = service;
		}

		public IDiscoverableService Service
		{
			get;
		}
	}

	public static class GettingStarted
	{
		public static event EventHandler<RoleQuestionEventArgs> AskingRole;
		public static event EventHandler RequestCreateCampaign;
		public static event EventHandler RequestJoinCampaign;
		public static event EventHandler<ServiceDiscoveredEventArgs> ServiceDiscovered;

		public static async Task StartAsync (CompositionHost composition, Task uiReady)
		{
			await uiReady;
			await RunRoleSetupAsync ();
			await RunServiceDiscoveryAsync (composition);
		}

		private static async Task RunRoleSetupAsync()
		{
			var isGmQuestion = new RoleQuestionEventArgs ();
			AskingRole?.Invoke (null, isGmQuestion);
			if (isGmQuestion.IsGameMaster == null)
				return;

			bool isGm = await isGmQuestion.IsGameMaster;

			if (isGm)
				await RunGmSetupAsync ();
			else
				await RunPlayerSetupAsync ();
		}

		private static async Task RunGmSetupAsync()
		{
			RequestCreateCampaign?.Invoke (null, EventArgs.Empty);
		}

		private static async Task RunPlayerSetupAsync()
		{
			RequestJoinCampaign?.Invoke (null, EventArgs.Empty);
		}

		private static Task RunServiceDiscoveryAsync (CompositionHost composition)
		{
			return Task.Run (async () => {
				IDiscoverableService[] services = composition.GetExports<IService> ().OfType<IDiscoverableService> ().ToArray ();

				var results = new Dictionary<Task<bool>, IDiscoverableService> (services.Length);
				for (int i = 0; i < services.Length; i++) {
					results.Add (services[i].DiscoverAsync (), services[i]);
				}

				while (results.Count > 0) {
					Task<bool> completed = await Task.WhenAny (results.Keys);
					IDiscoverableService service = results[completed];
					results.Remove (completed);

					ServiceDiscovered?.Invoke (null, new ServiceDiscoveredEventArgs (service));
				}
			});
		}
	}
}