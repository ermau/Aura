using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
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
				// this messege and start connecting to it.
				if (!TryNavigateToPage (PlayPageName))
					return;

				Messenger.Default.Send (cc);
			});

			Messenger.Default.Register<PromptMessage> (this, p => p.Result = OnPromptAsync (p));

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
			this.vm = new AppViewModel (await services.GetServiceAsync<ISyncService> (), await services.GetServiceAsync<CampaignManager>(), await services.GetServiceAsync<PlaySpaceManager>());
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
			this.contentFrame.Navigate (PageMap[tag]);

			ApplicationData.Current.LocalSettings.Values[NavPageName] = tag;
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
			{ PlayPageName, typeof(JoinGamePage) },
			{ "effects", typeof(EffectsPage) }
		};

		private Flyout dragFlyout;
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

		private void OnDragEnter (object sender, DragEventArgs e)
		{
			HideDragFlyout ();

			if (AttemptShowConnect (e))
				return;

			if (e.DataView.AvailableFormats.Contains (StandardDataFormats.StorageItems)) {
				e.AcceptedOperation = DataPackageOperation.Copy;
				e.DragUIOverride.IsCaptionVisible = false;

				this.dragFlyout = (Flyout)Resources["ImportFlyout"];
				this.dragFlyout.ShowAt (this.contentFrame, new FlyoutShowOptions {
					Position = new Point (this.contentFrame.ActualWidth / 2, Inset),
					ShowMode = FlyoutShowMode.Transient
				});
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

		private void OnDrop (object sender, DragEventArgs e)
		{
			if (e.DataView.CouldHaveLink()) {
				HideDragFlyout ();
				PromptConnect (e);
				return;
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

		private void OnEditCampaigns (object sender, RoutedEventArgs e)
		{
			this.nav.SelectedItem = this.campaignsNav;
		}

		private void OnEditPlayspaces (object sender, RoutedEventArgs e)
		{
			this.nav.SelectedItem = this.playspacesNav;
		}
	}
}
