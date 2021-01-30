using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Aura.Service;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Aura
{
	internal static class WinExtensions
	{
		public static ConfiguredTaskAwaitable<T> ConfigureAwait<T> (this IAsyncOperation<T> self, bool useSyncContext)
		{
			if (self == null)
				throw new ArgumentNullException (nameof (self));

			return self.AsTask ().ConfigureAwait (useSyncContext);
		}

		public static T FindParent<T> (this DependencyObject self)
		{
			DependencyObject parent = self;
			while (parent != null) {
				if (parent is T found)
					return found;

				parent = VisualTreeHelper.GetParent (parent);
			}

			return default;
		}

		public static Popup FindActiveFlyout (this DependencyObject self)
		{
			var popups = VisualTreeHelper.GetOpenPopups (Window.Current);
			foreach (Popup popup in popups) {
				if (popup.Child is FlyoutPresenter flyout) {
					if (flyout.Content == self)
						return popup;
				}
			}

			return null;
		}

		public static bool CouldHaveLink(this DataPackageView self)
		{
			return (self.AvailableFormats.Contains (StandardDataFormats.Text) || self.AvailableFormats.Contains (StandardDataFormats.WebLink));
		}

		public static async Task<(string Url, bool IsConnect)> TryGetLinkAsync (this DataPackageView self)
		{
			if (self.AvailableFormats.Contains (StandardDataFormats.Text)) {
				string text = await self.GetTextAsync ();
				if (LiveCampaignClient.IsLiveUri (text))
					return (text, LiveCampaignClient.IsConnectUri(text));
			}

			if (self.AvailableFormats.Contains (StandardDataFormats.WebLink)) {
				Uri uri = await self.GetWebLinkAsync ();
				if (LiveCampaignClient.IsLiveUri (uri))
					return (uri.ToString (), LiveCampaignClient.IsConnectUri (uri));
			}

			return (null, false);
		}
	}
}
