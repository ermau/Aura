using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Aura
{
	internal static class FlyoutService
	{
		public static void RegisterFlyoutTarget (FrameworkElement element)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));
			if (TargetElement != null)
				throw new InvalidOperationException ("Can not register more than one flyout target");

			TargetElement = element;

			foreach ((string name, object context) in QueuedFlyouts)
				PushFlyout (name, context);

			QueuedFlyouts = null;
		}

		public static void PushFlyout (string flyoutName, object context)
		{
			if (flyoutName == null)
				throw new ArgumentNullException (nameof (flyoutName));

			if (TargetElement == null) {
				QueuedFlyouts.Enqueue ((flyoutName, context));
				return;
			}

			var oldFlyout = CurrentFlyout;
			Flyouts.Push (oldFlyout);

			var flyout = GetFlyout (flyoutName, context);
			SwapFlyouts (oldFlyout, flyout);
		}

		private static Queue<(string, object)> QueuedFlyouts = new Queue<(string, object)> ();
		private static readonly Stack<Flyout> Flyouts = new Stack<Flyout> ();
		private static Flyout CurrentFlyout;
		private static FrameworkElement TargetElement;

		private static Flyout GetFlyout (string name, object context)
		{
			var flyout = (Flyout)App.Current.Resources[name];
			((FrameworkElement)flyout.Content).DataContext = context;

			return flyout;
		}

		private static void SwapFlyouts (Flyout oldFlyout, Flyout newFlyout)
		{
			if (oldFlyout != null) {
				oldFlyout.Closed -= OnFlyoutClosed;
				oldFlyout.Hide ();
				((FrameworkElement)oldFlyout.Content).DataContext = null;
			}

			if (newFlyout != null) {
				newFlyout.Closed += OnFlyoutClosed;
				CurrentFlyout = newFlyout;
				CurrentFlyout.ShowAt (TargetElement, new FlyoutShowOptions {
					Position = new Point (TargetElement.ActualWidth / 2, 40),
					ShowMode = FlyoutShowMode.Transient
				});
			}
		}

		private static void OnFlyoutClosed (object sender, object e)
		{
			var closed = (Flyout)sender;
			closed.Closed -= OnFlyoutClosed;

			if (Flyouts.TryPop (out Flyout flyout)) {
				SwapFlyouts (closed, flyout);
			}
		}
	}
}
