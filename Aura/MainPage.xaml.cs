using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;
using Aura.Messages;
using Aura.Service;
using Aura.Service.Client;
using Aura.ViewModels;

using GalaSoft.MvvmLight.Messaging;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

			SystemNavigationManager.GetForCurrentView ().BackRequested += OnBackRequested;
			Window.Current.CoreWindow.PointerPressed += OnPointerPressed;
			Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += OnAcceleratorKeyActivated;

			Messenger.Default.Register<RequestJoinCampaignPromptMessage> (this, async rj => {
				var join = new JoinCampaignDialog ();
				await join.ShowAsync ();
			});

			Messenger.Default.Register<RequestCreateCampaignPromptMessage> (this, async rc => {
				var create = new CreateCampaignDialog ();
				await create.ShowAsync ();
			});

			Messenger.Default.Register<CampaignDisconnectedMessage> (this, cd => {
				FlyoutService.PushFlyout ("LostConnectionFlyout", null);
			});

			Messenger.Default.Register<CampaignReconnectedMessage> (this, cr => {
				FlyoutService.SwapFlyout ("LostConnectionFlyout", "ReconnectedFlyout", null);
			});

			Messenger.Default.Register<ConnectCampaignMessage> (this, cc => {
				// If the play page has already been created, this will swap to it, but it will have heard
				// this message and start connecting to it.
				if (!TryNavigateToPage (PlayPageName))
					return;

				Messenger.Default.Send (cc);
			});

			Messenger.Default.Register<PromptMessage> (this, p => p.Result = OnPromptAsync (p));

			Messenger.Default.Register<NavigateToElementMessage> (this, OnNavigateToElement);

			FlyoutService.RegisterFlyoutTarget (this.contentFrame);
			Clipboard.ContentChanged += OnClipboardContentChanged;

			Loaded += MainPage_Loaded;
		}

		private async Task<bool> OnPromptAsync (PromptMessage msg)
		{
			var dialog = new ContentDialog {
				Title = msg.Title,
				Content = msg.Message,
				PrimaryButtonText = msg.PositiveAction,
				DefaultButton = ContentDialogButton.Secondary,
				SecondaryButtonText = "Cancel"
			};

			var result = await dialog.ShowAsync ();
			return (result == ContentDialogResult.Primary);
		}

		private AppViewModel vm;

		private async void SetupDataContext (IAsyncServiceProvider services)
		{
			this.vm = new AppViewModel (
				await services.GetServiceAsync<ISyncService> (),
				await services.GetServiceAsync<DownloadManager>(),
				await services.GetServiceAsync<CampaignManager>(),
				await services.GetServiceAsync<PlaySpaceManager>());
			DataContext = this.vm;
			((INotifyCollectionChanged)this.vm.Campaigns.Elements).CollectionChanged += OnCampaignsChanged;
			OnCampaignsChanged (null, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
			((INotifyCollectionChanged)this.vm.PlaySpaces.Elements).CollectionChanged += OnPlaySpacesChanged;
			OnPlaySpacesChanged (null, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		private void UpdateMenu<T> (NavigationViewItem navItem, Symbol symbol, NotifyCollectionChangedEventArgs e, SingleSelectionManager<T> manager, T emptyElement = null)
			where T : NamedElement
		{
			var menu = (MenuFlyout)navItem.ContextFlyout;
			int sepIndex = menu.Items.IndexOf (menu.Items.OfType<MenuFlyoutSeparator> ().First ());
			int i;
			for (i = 0; i < sepIndex; i++) {
				menu.Items.RemoveAt (0);
			}

			if (manager.Elements.Count == 0) {
				if (emptyElement != null) {
					menu.Items.Insert (0, new MenuFlyoutItem {
						Text = emptyElement.Name,
						IsEnabled = false
					});
				}

				return;
			}

			i = 0;
			foreach (T element in manager.Elements) {
				var radio = new Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem {
					DataContext = new SingleSelectionItemViewModel<T> (manager, element),
					Icon = new SymbolIcon (symbol)
				};

				radio.SetBinding (MenuFlyoutItem.TextProperty, new Binding { Path = new PropertyPath ("Name"), Mode = BindingMode.OneTime });
				radio.SetBinding (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem.IsCheckedProperty, new Binding { Path = new PropertyPath (nameof(SingleSelectionItemViewModel<NamedElement>.IsSelected)), Mode = BindingMode.TwoWay });

				menu.Items.Insert (i++, radio);
			}
		}

		private async void OnPlaySpacesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			await Dispatcher.RunAsync (CoreDispatcherPriority.Normal, () =>
				UpdateMenu (this.playspacesNav, Symbol.Home, e, this.vm.PlaySpaces));
		}

		private async void OnCampaignsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			await Dispatcher.RunAsync (CoreDispatcherPriority.Normal, () =>
				UpdateMenu (this.campaignsNav, Symbol.World, e, this.vm.Campaigns, new CampaignElement { Name = "No campaigns" }));
		}

		private const string NavPageName = "NavPage";
		private void MainPage_Loaded (object sender, RoutedEventArgs e)
		{
			if (ApplicationData.Current.LocalSettings.Values.TryGetValue (NavPageName, out object navObj) && navObj is string tag) {
				TryNavigateToPage (tag);
			}
		}

		private bool TryNavigateToPage (string page)
		{
			var navItem = this.nav.MenuItems.OfType<NavigationViewItem> ().FirstOrDefault (n => (string)n.Tag == page);
			if (navItem != null) {
				navItem.IsSelected = true;
				return true;
			} else
				return false;
		}

		private void OnNavigationSelectionChanged (NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			string tag = (!args.IsSettingsSelected) ? (string)args.SelectedItemContainer.Tag : "settings";
			ApplicationData.Current.LocalSettings.Values[NavPageName] = tag;

			if (this.isNavigating)
				return;

			this.contentFrame.Navigate (PageMap[tag]);
		}

		private void OnHomeTapped (object sender, TappedRoutedEventArgs e)
		{
			((FrameworkElement)sender).ContextFlyout.ShowAt ((FrameworkElement)sender);
		}

		private void OnCampaignTapped (object sender, TappedRoutedEventArgs e)
		{
			((FrameworkElement)sender).ContextFlyout.ShowAt ((FrameworkElement)sender);
		}

		private const string PlayPageName = "play";

		private static readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type> {
			{ "settings", typeof(SettingsPage) },
			{ "layers", typeof(LayersPage) },
			{ "encounters", typeof(EncountersPage) },
			//{ "playlists", typeof(PlaylistsPage) },
			{ "elements", typeof(ElementsPage) },
			{ "campaigns", typeof(EditCampaignsPage) },
			{ "playspaces", typeof(PlaySpacesPage) },
			{ PlayPageName, typeof(PlayGamePage) },
			{ "effects", typeof(EffectsPage) },
			{ "samples", typeof(SamplesPage) }
		};

		private static readonly Dictionary<Type, string> ElementMap = new Dictionary<Type, string> {
			{ typeof(FileSample), "samples" },
			{ typeof(EncounterElement), "encounters" },
			{ typeof(LayerElement), "layers" },
			{ typeof(CampaignElement), "campaigns" },
			{ typeof(PlaySpaceElement), "playspaces" },
			{ typeof(EnvironmentElement), "elements" }
		};

		private bool isNavigating;
		private IDisposable dragFlyout;
		private CancellationTokenSource clipboardCampaignCancel;

		private async void OnClipboardContentChanged (object sender, object e)
		{
			DataPackageView dataPackageView = Clipboard.GetContent ();
			(string url, bool isJoin) = await dataPackageView.TryGetLinkAsync ();
			if (url == null)
				return;

			PromptConnect (url, isJoin);
		}

		private const int Inset = 70;

		private async void OnDragEnter (object sender, DragEventArgs e)
		{
			if (AttemptShowConnect (e))
				return;

			if (e.DataView.AvailableFormats.Contains (StandardDataFormats.StorageItems)) {
				e.AcceptedOperation = DataPackageOperation.Copy;
				e.DragUIOverride.IsCaptionVisible = false;

				var items = await e.DataView.GetStorageItemsAsync ();
				bool many = (items.Count > 1 || items.OfType<IStorageFolder> ().Any ());

				// TODO: localize
				string message = $"Drop to import {((!many) ? items[0].Name : "files")}";
				string glyph = (many) ? "\xE8B6" : "\xE8B5";
				IDisposable old = Interlocked.Exchange (ref this.dragFlyout, FlyoutService.ShowMessage (message, glyph));
				old?.Dispose ();
			}
		}

		private bool AttemptShowConnect(DragEventArgs e)
		{
			if (!e.DataView.CouldHaveLink())
				return false;

			e.AcceptedOperation = DataPackageOperation.Link;
			e.DragUIOverride.IsCaptionVisible = false;

			ShowConnect (e);

			return true;
		}

		private async void ShowConnect (DragEventArgs e)
		{
			(string url, bool isConnect) = await e.DataView.TryGetLinkAsync ();
			if (url == null)
				return;

			ShowConnect (url, isConnect);
		}

		private async void ShowConnect (string url, bool isConnect)
		{
			var client = await App.Services.GetServiceAsync<ILiveCampaignClient> ();

			try {
				var cancel = new CancellationTokenSource (5000);
				var details = await client.GetCampaignDetailsAsync (url, cancel.Token);
				
				string message = (isConnect)
					? $"Drop to connect to {details.Name}"
					: $"Drop to join {details.Name}";

				string glyph = (isConnect)
					? "\xE768"
					: "\xE71B";

				FlyoutService.ShowMessage (message, glyph);
			} catch (OperationCanceledException) {
			}
		}

		private void OnDragLeave (object sender, DragEventArgs e)
		{
			HideDragFlyout ();
		}

		private void HideDragFlyout()
		{
			IDisposable flyout = Interlocked.Exchange (ref this.dragFlyout, null);
			flyout?.Dispose ();
		}

		private async void PromptConnect(DragEventArgs e)
		{
			(string url, bool isConnect) = await e.DataView.TryGetLinkAsync ();
			if (url == null)
				return;

			PromptConnect (url, isConnect);
		}

		private async void PromptConnect(string url, bool isConnect)
		{
			var newSource = new CancellationTokenSource (15000);
			var oldSource = Interlocked.Exchange (ref this.clipboardCampaignCancel, newSource);
			oldSource?.Cancel ();

			var client = await App.Services.GetServiceAsync<ILiveCampaignClient> ();
			try {
				Task<RemoteCampaign> campaignTask = client.GetCampaignDetailsAsync (new Uri (url), newSource.Token);

				var campaigns = await App.Services.GetServiceAsync<CampaignManager> ();
				var campaign = await campaignTask;
				if (campaign == null || campaigns.Elements.Any (c => c.Id == campaign.id.ToString ()))
					return;

				if (!isConnect) {
					FlyoutService.PushFlyout ("CampaignConnectFlyout", new JoinCampaignRequestViewModel (campaign));
				}
			} catch (OperationCanceledException) {
			}
		}

		private async void OnDrop (object sender, DragEventArgs e)
		{
			HideDragFlyout ();
			if (e.DataView.CouldHaveLink()) {
				PromptConnect (e);
				return;
			} else if (e.DataView.Contains (StandardDataFormats.StorageItems)) {
				var items = await e.DataView.GetStorageItemsAsync ();
				Task importTask = Importer.ImportAsync (items);
				TryNavigateToPage ("samples");
				await importTask;
			}
		}

		private void SearchQuerySubmitted (AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			SearchResultItem result = args.ChosenSuggestion as SearchResultItem;
			if (result?.Element == null)
				return;

			OnNavigateToElement (new NavigateToElementMessage (result.Element.Id, result.Element.GetType ()));
			sender.Text = String.Empty;
		}

		private void SearchTextChanged (AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				this.vm.SearchQuery = sender.Text;
		}

		private void OnJoinCampaign (object sender, RoutedEventArgs e)
		{
			Messenger.Default.Send (new RequestJoinCampaignPromptMessage ());
		}

		private void OnEditCampaigns (object sender, RoutedEventArgs e)
		{
			this.nav.SelectedItem = this.campaignsNav;
		}

		private void OnEditPlayspaces (object sender, RoutedEventArgs e)
		{
			this.nav.SelectedItem = this.playspacesNav;
		}

		private void OnBackRequested (object sender, BackRequestedEventArgs e)
		{
			e.Handled = TryGoBack ();
		}

		private void OnAcceleratorKeyActivated (CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			// When Alt+Left are pressed navigate back.
			// When Alt+Right are pressed navigate forward.
			if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown
				&& (args.VirtualKey == VirtualKey.Left || args.VirtualKey == VirtualKey.Right)
				&& args.KeyStatus.IsMenuKeyDown == true
				&& !args.Handled) {

				if (args.VirtualKey == VirtualKey.Left) {
					args.Handled = TryGoBack ();
				} else if (args.VirtualKey == VirtualKey.Right) {
					args.Handled = TryGoForward ();
				}
			}
		}

		private void OnPointerPressed (CoreWindow sender, PointerEventArgs args)
		{
			// For this event, e.Handled arrives as 'true', so invert the value.
			if (args.CurrentPoint.Properties.IsXButton1Pressed && args.Handled) {
				args.Handled = !TryGoBack ();
			} else if (args.CurrentPoint.Properties.IsXButton2Pressed && args.Handled) {
				args.Handled = TryGoForward ();
			}
		}

		private void OnBackRequested (NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			TryGoBack ();
		}

		private bool TryGoBack()
		{
			if (!this.contentFrame.CanGoBack)
				return false;

			this.contentFrame.GoBack ();
			UpdateSelectionForCurrentContent ();
			return true;
		}

		private bool TryGoForward()
		{
			if (!this.contentFrame.CanGoForward)
				return false;

			this.contentFrame.GoForward ();
			UpdateSelectionForCurrentContent ();
			return true;
		}

		private void UpdateSelectionForCurrentContent()
		{
			this.isNavigating = true;

			Type contentType = this.contentFrame.Content.GetType ();
			KeyValuePair<string, Type> content = PageMap.FirstOrDefault (kvp => kvp.Value == contentType);
			if (!content.Equals (default)) {
				NavigationViewItem item = this.nav.MenuItems.Concat (this.footerItems.Children).OfType<NavigationViewItem> ().FirstOrDefault (nvi => nvi.Tag.Equals (content.Key));
				if (item != null)
					this.nav.SelectedItem = item;
				else if (content.Key == "settings")
					this.nav.SelectedItem = this.nav.SettingsItem;
			}

			this.isNavigating = false;
		}

		private void OnNavigateToElement (NavigateToElementMessage msg)
		{
			if (!ElementMap.TryGetValue (msg.Type, out string tag)) {
				Trace.WriteLine ($"Could not find element type for {msg.Type} to navigate");
			}

			Type pageType = PageMap[tag];
			this.contentFrame.Navigate (pageType, msg.Id);
			this.isNavigating = true;
			TryNavigateToPage (tag);
			this.isNavigating = false;
		}
	}
}
