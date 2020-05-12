using System;
using System.Collections.Generic;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Aura
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnNavigationSelectionChanged (NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                this.contentFrame.Navigate (typeof (SettingsPage));
            else {
                this.contentFrame.Navigate (PageMap[(string)sender.Tag]);
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
            { "layers", typeof(LayersPage) },
            { "encounters", typeof(EncountersPage) },
            { "playlists", typeof(PlaylistsPage) },
            { "elements", typeof(ElementsPage) },
            { "campaigns", typeof(EditCampaignsPage) },
            { "playspaces", typeof(EditPlayspacesPage) }
        };
    }
}
