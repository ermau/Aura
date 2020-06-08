//
// FlyoutService.cs
//
// Authors:
//       Eric Maupin <me@ermau.com>
//
// Copyright (c) 2020 Eric Maupin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Aura
{
	internal static class FlyoutService
	{
		public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.RegisterAttached ("IsVisible", typeof (bool), typeof (FlyoutService), new PropertyMetadata (true, IsOpenChangedCallback));

		public static void SetIsVisible (DependencyObject element, bool value)
		{
			element.SetValue (IsVisibleProperty, value);
		}

		public static bool GetIsVisible (DependencyObject element)
		{
			return (bool)element.GetValue (IsVisibleProperty);
		}

		private static void IsOpenChangedCallback (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Popup flyout = d.FindActiveFlyout ();
			if (flyout == null)
				return;

			if ((bool)e.NewValue) {
				flyout.Closed += OnFlyoutClosed;
			} else {
				flyout.Closed -= OnFlyoutClosed;
				flyout.IsOpen = false;
			}
		}

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

			CurrentFlyout = newFlyout;
			if (newFlyout != null) {
				newFlyout.Closed += OnFlyoutClosed;
				CurrentFlyout.ShowAt (TargetElement, new FlyoutShowOptions {
					Position = new Point (TargetElement.ActualWidth / 2, 40),
					ShowMode = FlyoutShowMode.Transient
				});
			}
		}

		private static void OnFlyoutClosed (object sender, object e)
		{
			SetIsVisible (((Flyout)sender).Content, false);

			Flyout newFlyout = null;
			var closed = (Flyout)sender;
			if (CurrentFlyout == closed)
				Flyouts.TryPop (out newFlyout);

			SwapFlyouts (closed, newFlyout);
		}
	}
}
