using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Messages;
using Aura.Service;
using Aura.Service.Client;
using Aura.ViewModels;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace Aura
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			InitializeComponent();
			SetupDataContext (App.Services);

			Messenger.Default.Register<RequestJoinCampaignPromptMessage> (this, async rj => {
				var join = new JoinCampaignDialog ();
				await join.ShowAsync ();
			});

			FlyoutService.RegisterFlyoutTarget (this.contentFrame);

			var coreTitleBar = CoreApplication.GetCurrentView ().TitleBar;
			coreTitleBar.ExtendViewIntoTitleBar = true;
			UpdateTitleBarLayout (coreTitleBar);

			Window.Current.SetTitleBar (AppTitleBar);
			coreTitleBar.LayoutMetricsChanged += (sender, args) => UpdateTitleBarLayout (sender);
			coreTitleBar.IsVisibleChanged += (sender, args) => AppTitleBar.Visibility = (sender.IsVisible) ? Visibility.Visible : Visibility.Collapsed;

			Clipboard.ContentChanged += OnClipboardContentChanged;
		}

		private AppViewModel vm;

		private async void SetupDataContext (IAsyncServiceProvider services)
		{
			this.vm = new AppViewModel (await services.GetServiceAsync<ISyncService> (), await services.GetServiceAsync<CampaignManager>(), await services.GetServiceAsync<PlaySpaceManager>());
			DataContext = this.vm;
			((INotifyCollectionChanged)this.vm.Campaigns.Elements).CollectionChanged += OnCampaignsChanged;
			OnCampaignsChanged (null, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
			((INotifyCollectionChanged)this.vm.PlaySpaces.Elements).CollectionChanged += OnPlaySpacesChanged;
			OnPlaySpacesChanged (null, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		private void UpdateMenu<T> (NavigationViewItem navItem, IconElement elementIcon, NotifyCollectionChangedEventArgs e, SingleSelectionManager<T> manager)
			where T : NamedElement
		{
			var menu = (MenuFlyout)navItem.ContextFlyout;
			int sepIndex = menu.Items.IndexOf (menu.Items.OfType<MenuFlyoutSeparator> ().First ());
			int i;
			for (i = 0; i < sepIndex; i++) {
				menu.Items.RemoveAt (0);
			}

			i = 0;
			foreach (T element in manager.Elements) {
				var radio = new Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem {
					DataContext = new SingleSelectionItemViewModel<T> (manager, element),
					Icon = elementIcon,
				};

				radio.SetBinding (MenuFlyoutItem.TextProperty, new Binding { Path = new PropertyPath ("Name"), Mode = BindingMode.OneTime });
				radio.SetBinding (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem.IsCheckedProperty, new Binding { Path = new PropertyPath (nameof(SingleSelectionItemViewModel<NamedElement>.IsSelected)), Mode = BindingMode.TwoWay });

				this.campaignsMenu.Items.Insert (i++, radio);
			}
		}

		private void OnPlaySpacesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateMenu (this.playspacesNav, new SymbolIcon (Symbol.Home), e, this.vm.PlaySpaces);
		}

		private void OnCampaignsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateMenu (this.campaignsNav, new SymbolIcon (Symbol.World), e, this.vm.Campaigns);
		}

		private void UpdateTitleBarLayout (CoreApplicationViewTitleBar title)
		{
			LeftPaddingColumn.Width = new GridLength (title.SystemOverlayLeftInset);
			RightPaddingColumn.Width = new GridLength (title.SystemOverlayRightInset);
			AppTitleBar.Height = title.Height;
		}

		private void OnNavigationSelectionChanged (NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.IsSettingsSelected)
				this.contentFrame.Navigate (typeof (SettingsPage));
			else {
				this.contentFrame.Navigate (PageMap[(string)args.SelectedItemContainer.Tag]);
			}
		}

		private void OnHomeTapped (object sender, TappedRoutedEventArgs e)
		{
			((FrameworkElement)sender).ContextFlyout.ShowAt ((FrameworkElement)sender);
		}

		private void OnCampaignTapped (object sender, TappedRoutedEventArgs e)
		{
			((FrameworkElement)sender).ContextFlyout.ShowAt ((FrameworkElement)sender);
		}

		private static readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type> {
			{ "play", typeof(PlayPage) },
			{ "layers", typeof(LayersPage) },
			{ "encounters", typeof(EncountersPage) },
			{ "playlists", typeof(PlaylistsPage) },
			{ "elements", typeof(ElementsPage) },
			{ "campaigns", typeof(EditCampaignsPage) },
			{ "playspaces", typeof(EditPlayspacesPage) }
		};

		private Flyout dragFlyout;
		private CancellationTokenSource clipboardCampaignCancel;

		private async void OnClipboardContentChanged (object sender, object e)
		{
			DataPackageView dataPackageView = Clipboard.GetContent ();
			string url = await dataPackageView.TryGetJoinLinkAsync ();
			if (url == null)
				return;
			
			var newSource = new CancellationTokenSource (15000);
			var oldSource = Interlocked.Exchange (ref this.clipboardCampaignCancel, newSource);
			oldSource?.Cancel ();

			var client = await App.Services.GetServiceAsync<ILiveCampaignClient> ();
			try {
				RemoteCampaign campaign = await client.GetCampaignDetailsAsync (new Uri (url), newSource.Token);
				if (campaign == null)
					return;

				var campaigns = await App.Services.GetServiceAsync<CampaignManager> ();
				if (campaigns.Elements.Any (c => c.Id == campaign.id.ToString ()))
					return;

				FlyoutService.PushFlyout ("CopyConnectFlyout", new JoinCampaignRequestViewModel (campaign));
			} catch (OperationCanceledException) {
			}
		}

		private void OnDragEnter (object sender, DragEventArgs e)
		{
			HideDragFlyout ();

			if (e.DataView.AvailableFormats.Contains (StandardDataFormats.WebLink)) {
				Uri uri = e.DataView.GetWebLinkAsync ().AsTask().Result;
				if (LiveCampaignClient.IsLiveUri (uri)) {
					e.AcceptedOperation = DataPackageOperation.Link;
					e.DragUIOverride.IsCaptionVisible = false;
				}
			} else if (e.DataView.AvailableFormats.Contains (StandardDataFormats.StorageItems)) {
				e.AcceptedOperation = DataPackageOperation.Copy;
				e.DragUIOverride.IsCaptionVisible = false;

				this.dragFlyout = (Flyout)Resources["ImportFlyout"];
				this.dragFlyout.ShowAt (this.contentFrame, new FlyoutShowOptions {
					Position = new Point (this.contentFrame.ActualWidth / 2, 40),
					ShowMode = FlyoutShowMode.Transient
				});
			} else if (e.DataView.AvailableFormats.Contains (StandardDataFormats.Text)) {
				string text = e.DataView.GetTextAsync ().AsTask().Result;
				if (LiveCampaignClient.IsLiveUri (text)) {
					e.AcceptedOperation = DataPackageOperation.Link;
					e.DragUIOverride.IsCaptionVisible = false;

					this.dragFlyout = (Flyout)Resources["ConnectFlyout"];
					this.dragFlyout.ShowAt (this.contentFrame, new FlyoutShowOptions {
						Position = new Point (this.contentFrame.ActualWidth / 2, 40),
						ShowMode = FlyoutShowMode.Transient
					});
				}
			}
		}

		private void HideDragFlyout()
		{
			if (this.dragFlyout != null) {
				this.dragFlyout.Hide ();
				this.dragFlyout = null;
			}
		}

		private void OnDragLeave (object sender, DragEventArgs e)
		{
			HideDragFlyout ();
		}

		private void OnDrop (object sender, DragEventArgs e)
		{
			if (e.DataView.AvailableFormats.Contains (StandardDataFormats.StorageItems)) {
				var ch = ((Grid)this.dragFlyout.Content).Children;
				((TextBlock)ch[1]).Text = "Importing package...";
				var progress = ((ProgressBar)ch[2]);
				progress.Visibility = Visibility.Visible;

				Task.Delay (2000).ContinueWith (async t => {
					progress.IsIndeterminate = false;
					for (int i = 0; i <= 100; i++) {
						await Task.Delay (150);
						progress.Value = i;
					}
				}, TaskScheduler.FromCurrentSynchronizationContext ());
			} else
				HideDragFlyout ();
		}

		private async Task<bool> ImportStorageItemAsync (IStorageItem item)
		{
			if (item is IStorageFolder folder) {
				/*var items = await folder.GetItemsAsync ();
				foreach (IStorageItem folderItem in items) {
					if (!await ValidateStorageItemAsync (folderItem))
						return false;
				}*/

				return true;
			} else if (item is IStorageFile file) {
				return (file.Name.EndsWith (".aura"));
			} else {
				throw new InvalidOperationException ();
			}
		}

		private void SearchQuerySubmitted (AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			this.vm.SearchQuery = args.QueryText;
		}

		private void SearchTextChanged (AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				this.vm.SearchQuery = sender.Text;
		}

		private async void OnJoinCampaign (object sender, RoutedEventArgs e)
		{
			Messenger.Default.Send (new RequestJoinCampaignPromptMessage ());
		}
	}
}
