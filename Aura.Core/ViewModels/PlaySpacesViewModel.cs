using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class PlaySpacesViewModel
		: EditSingleSelectionItemsViewModel<PlaySpaceElement, EditPlaySpaceViewModel>
	{
		public PlaySpacesViewModel(IAsyncServiceProvider services)
			: base (services)
		{
		}

		protected override PlaySpaceElement InitializeElement (string name)
		{
			return new PlaySpaceElement { Name = name };
		}

		protected override EditSingleSelectionElementViewModel<PlaySpaceElement> InitializeElementViewModel (PlaySpaceElement element)
		{
			return new EditPlaySpaceViewModel (ServiceProvider, SyncService, element);
		}

		protected override async Task<SingleSelectionManager<PlaySpaceElement>> GetManagerAsync ()
		{
			return await ServiceProvider.GetServiceAsync<PlaySpaceManager> ();
		}
	}

	internal class EditPlaySpaceViewModel
		: EditSingleSelectionElementViewModel<PlaySpaceElement>
	{
		public EditPlaySpaceViewModel (IAsyncServiceProvider services, ISyncService sync, PlaySpaceElement element)
			: base (services, sync, element)
		{
		}

		public string Name
		{
			get => ModifiedElement.Name;
			set
			{
				if (Name == value)
					return;

				ModifiedElement = ModifiedElement with { Name = value };
			}
		}

		public IReadOnlyList<LightingServiceEntry> LightingServiceEntries => this.lightServiceEntries;

		public LightingServiceEntry SelectedLightingService
		{
			get => this.selectedLightingService;
			set
			{
				if (this.selectedLightingService == value)
					return;

				this.selectedLightingService = value;
				RaisePropertyChanged ();
				LoadLighting ();
			}
		}

		public bool LightingLoading
		{
			get => this.lightingLoading;
			set
			{
				if (this.lightingLoading == value)
					return;

				this.lightingLoading = value;
				RaisePropertyChanged ();
				RaisePropertyChanged (nameof (ShowLightingServiceConfiguration));
				RaisePropertyChanged (nameof (ShowLightingPairingConfiguration));
			}
		}

		public IReadOnlyList<LightGroup> LightingGroups => this.lightGroups;

		public LightGroup SelectedLightGroup
		{
			get => this.selectedLightGroup;
			set
			{
				if (this.selectedLightGroup == value)
					return;

				this.selectedLightGroup = value;
				RaisePropertyChanged ();

				SelectedLightingConfiguration = SelectedLightingConfiguration with { RoomId = value?.Id };
			}
		}

		public bool ShowLightingServiceConfiguration
		{
			get => !LightingLoading && this.showLightConfig;
			private set
			{
				if (this.showLightConfig == value)
					return;

				this.showLightConfig = value;
				RaisePropertyChanged ();
			}
		}

		public bool ShowLightingPairingConfiguration
		{
			get => !LightingLoading && this.showLightPairing;
			private set
			{
				if (this.showLightPairing == value)
					return;

				this.showLightPairing = value;
				RaisePropertyChanged ();
			}
		}

		protected override void OnModified ()
		{
			base.OnModified ();
			RaisePropertyChanged (nameof (Name));

			string selectedServiceName = SelectedLightingService?.Service.GetType ().GetSimpleTypeName ();
			this.selectedLightingConfig = ModifiedElement.LightingConfigurations.FirstOrDefault (c => c.Service == selectedServiceName && c?.PairedDevice == SelectedLightingService.Device.Id)
					?? new LightingConfiguration ();
			this.selectedLightGroup = this.lightGroups?.FirstOrDefault (lg => lg.Id == this.selectedLightingConfig.RoomId);

			RaisePropertyChanged (nameof (SelectedLightGroup));
		}

		protected override async Task LoadAsync ()
		{
			this.lightServiceEntries.Clear ();
			await base.LoadAsync ();

			Task<ILightingService[]> lightingServicesTask = ServiceProvider.GetServicesAsync<ILightingService> ();

			LightingServiceEntry select = null;
			foreach (string serviceName in Element.Services) {
				Type type;
				try {
					type = Type.GetType (serviceName);
				} catch (Exception ex) {
					Trace.WriteLine ($"Error trying to load enabled service '{serviceName}': {ex}");
					continue;
				}

				if (!typeof (ILightingService).IsAssignableFrom (type))
					continue;

				var lightServices = await lightingServicesTask;
				var service = lightServices.Single (s => s.GetType() == type);
				this.lightServiceEntries.Add (new LightingServiceEntry (service));

				if (typeof (IPairedService).IsAssignableFrom (type)) {
					if (!Element.Pairings.TryGetValue (serviceName, out var pairings))
						continue;

					foreach (PairedDevice pairedDevice in pairings) {
						var entry = new LightingServiceEntry (service, pairedDevice);
						this.lightServiceEntries.Add (entry);

						if (select == null)
							select = entry;
					}
				}
			}

			SelectedLightingService = select ?? this.lightServiceEntries.FirstOrDefault ();
		}

		private readonly ObservableCollectionEx<LightingServiceEntry> lightServiceEntries = new ObservableCollectionEx<LightingServiceEntry>();
		private readonly ObservableCollectionEx<LightGroup> lightGroups = new ObservableCollectionEx<LightGroup> ();
		private LightingConfiguration selectedLightingConfig;
		private LightingServiceEntry selectedLightingService;
		private LightGroup selectedLightGroup;
		private bool lightingLoading, showLightPairing, showLightConfig;

		private CancellationTokenSource lightingCancel;

		private LightingConfiguration SelectedLightingConfiguration
		{
			get => this.selectedLightingConfig;
			set
			{
				if (this.selectedLightingConfig == value)
					return;

				var existing = this.selectedLightingConfig;
				this.selectedLightingConfig = value;

				var list = new List<LightingConfiguration> (ModifiedElement.LightingConfigurations);
				int index = list.IndexOf (existing);
				if (index == -1)
					list.Add (value);
				else
					list[index] = value;

				ModifiedElement = ModifiedElement with { LightingConfigurations = list };
				this.selectedLightGroup = this.lightGroups.FirstOrDefault (lg => lg.Id == value.RoomId);
				RaisePropertyChanged (nameof (SelectedLightGroup));
			}
		}

		private async void LoadLighting ()
		{
			var cancel = Interlocked.Exchange (ref this.lightingCancel, null);
			cancel?.Cancel ();

			this.lightGroups.Clear ();
			this.selectedLightingConfig = null;
			if (SelectedLightingService == null)
				return;

			string selectedServiceName = SelectedLightingService.Service.GetType ().GetSimpleTypeName ();

			if (SelectedLightingService.Device == null) {
				ShowLightingPairingConfiguration = true;
				ShowLightingServiceConfiguration = false;
				this.selectedLightingConfig = ModifiedElement.LightingConfigurations.FirstOrDefault (c => c.Service == selectedServiceName)
					?? new LightingConfiguration { Service = selectedServiceName };
				return;
			}

			cancel = new CancellationTokenSource ();
			if (Interlocked.CompareExchange (ref this.lightingCancel, cancel, null) != null)
				return;

			LightingLoading = true;
			ShowLightingPairingConfiguration = false;
			ShowLightingServiceConfiguration = false;

			try {
				ILightingService lightingService = SelectedLightingService.Service;
				var groups = await lightingService.GetGroupsAsync (cancel.Token);
				if (cancel.IsCancellationRequested)
					return;

				this.lightGroups.Reset (groups.OrderByDescending (g => g.IsEntertainment));

				ShowLightingServiceConfiguration = true;
				this.selectedLightingConfig = 
					ModifiedElement.LightingConfigurations.FirstOrDefault (c => c.Service == selectedServiceName && c.PairedDevice == SelectedLightingService.Device.Id)
					?? new LightingConfiguration { Service = selectedServiceName, PairedDevice = SelectedLightingService.Device.Id };

				this.selectedLightGroup = LightingGroups.FirstOrDefault (lg => lg.Id == SelectedLightingConfiguration.RoomId);
				RaisePropertyChanged (nameof (SelectedLightGroup));
			} catch (OperationCanceledException) {
			} finally {
				LightingLoading = false;
			}
		}
	}

	internal class LightingServiceEntry
	{
		public LightingServiceEntry (ILightingService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
		}

		public LightingServiceEntry (ILightingService service, PairedDevice device)
			: this (service)
		{
			Device = device ?? throw new ArgumentNullException (nameof (device));
		}

		public ILightingService Service
		{
			get;
		}

		public PairedDevice Device
		{
			get;
		}

		public override string ToString ()
		{
			return (Device != null)
				? Service.DisplayName + " - " + Device.Id
				: Service.DisplayName;
		}
	}
}
