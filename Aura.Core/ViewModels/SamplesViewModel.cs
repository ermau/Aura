using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class SamplesViewModel
		: ElementsViewModel<FileSample, SampleViewModel>
	{
		public SamplesViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			RequestReload ();
		}

		protected override SampleViewModel InitializeElementViewModel (FileSample element)
		{
			return new SampleViewModel (ServiceProvider, SyncService, element);
		}

		protected override FileSample InitializeElement (string name)
		{
			return new FileSample { Name = name };
		}
	}
	
	internal class SampleViewModel
		: ElementViewModel<FileSample>
	{
		public SampleViewModel(IAsyncServiceProvider services, ISyncService sync, FileSample element)
			: base (services, sync, element)
		{
		}

		public SampleViewModel(IAsyncServiceProvider services, ISyncService sync, string id)
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
			Name = Element.Name;
		}

		protected override async void OnDelete ()
		{
			base.OnDelete ();

			var storage = await ServiceProvider.GetServiceAsync<ILocalStorageService> ();
			await storage.DeleteAsync (Element.Id, Element.ContentHash);
		}
	}
}
