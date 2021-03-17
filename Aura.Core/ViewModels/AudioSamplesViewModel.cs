using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class AudioSamplesViewModel
		: ElementsViewModel<AudioSample, AudioSampleViewModel>
	{
		public AudioSamplesViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			RequestReload ();
		}

		protected override AudioSampleViewModel InitializeElementViewModel (AudioSample element)
		{
			return new AudioSampleViewModel (ServiceProvider, SyncService, element);
		}

		protected override AudioSample InitializeElement (string name)
		{
			return new AudioSample { Name = name };
		}
	}
	
	internal class AudioSampleViewModel
		: ElementViewModel<AudioSample>
	{
		public AudioSampleViewModel(IAsyncServiceProvider services, ISyncService sync, AudioSample element)
			: base (services, sync, element)
		{
		}

		public AudioSampleViewModel(IAsyncServiceProvider services, ISyncService sync, string id)
			: base (services, sync, id)
		{
		}

		public string Name
		{
			get => ModifiedElement.Name;
			set => ModifiedElement = ModifiedElement with { Name = value };
		}

		protected override void OnModified ()
		{
			base.OnModified ();
			RaisePropertyChanged (nameof (Name));
		}

		protected override async Task LoadAsync ()
		{
			await base.LoadAsync ();
			if (Element == null)
				return;

			Name = Element.Name;
		}

		protected override async void OnDelete ()
		{
			base.OnDelete ();

			var storage = await ServiceProvider.GetServiceAsync<ILocalStorageService> ();
			string id = Element.Id;
			await storage.DeleteAsync (id, Element.ContentHash);

			var elements = await SyncService.FindElementsAsync<EnvironmentElement> (ee => ee.Audio.Playlist.Descriptors.Contains (id));
			foreach (var ee in elements) {
				var descriptors = new List<string> (ee.Audio.Playlist.Descriptors);
				descriptors.Remove (id);

				var updated = ee with {
					Audio = ee.Audio with {
						Playlist = ee.Audio.Playlist with {
							Descriptors = descriptors
						}
					}
				};

				await SyncService.SaveElementAsync (ee);
			}
		}
	}
}
