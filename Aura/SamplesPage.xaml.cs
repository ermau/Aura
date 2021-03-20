using System;

using Aura.ViewModels;

using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	public sealed partial class SamplesPage : Page
	{
		public SamplesPage ()
		{
			DataContext = new AudioSamplesViewModel (App.Services);
			InitializeComponent ();
		}

		protected override void OnNavigatedTo (NavigationEventArgs e)
		{
			base.OnNavigatedTo (e);

			if (e.Parameter is string id) {
				((AudioSamplesViewModel)DataContext).RequestSelection (id);
			}
		}

		protected void OnFindContent (object sender, RoutedEventArgs e)
		{
			Frame.Navigate (typeof (ImportSamplePage));
		}

		private async void OnImportContent (object sender, RoutedEventArgs e)
		{
			var picker = new FileOpenPicker {
				SuggestedStartLocation = PickerLocationId.MusicLibrary,
			};
			picker.FileTypeFilter.AddRange (Importer.SupportedFiles);
			var files = await picker.PickMultipleFilesAsync ();

			await Importer.ImportAsync (files);
		}
	}
}
