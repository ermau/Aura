//
// FlyoutService.cs
//
// Authors:
//       Eric Maupin <me@ermau.com>
//
// Copyright (c) 2020-2021 Eric Maupin
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
using System.Linq;
using System.Threading;

using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

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

			foreach (FlyoutHandle handle in QueuedFlyouts)
				PushFlyout (handle);

			QueuedFlyouts = null;
		}

		public static IDisposable ShowMessage (string message, string glyph)
		{
			var context = new MessageFlyoutContext {
				Message = message,
				Glyph = glyph
			};

			return PushFlyout ("MessageFlyout", context);
		}

		public static void CloseMessage()
		{
			PopFlyout ("MessageFlyout");
		}

		public static IDisposable PushFlyout (string flyoutName, object context)
		{
			if (flyoutName == null)
				throw new ArgumentNullException (nameof (flyoutName));

			var handle = new FlyoutHandle (flyoutName, context);
			if (TargetElement == null) {
				QueuedFlyouts.Enqueue (handle);
				return handle;
			}

			var oldFlyout = CurrentFlyout;
			Flyouts.Add (oldFlyout);

			var flyout = GetFlyout (handle);
			SwapFlyouts (oldFlyout, flyout);

			return handle;
		}

		public static void PopFlyout (string flyoutName)
		{
			if (flyoutName is null)
				throw new ArgumentNullException (nameof (flyoutName));

			var old = (Flyout)App.Current.Resources[flyoutName];
			Flyouts.Remove (old);
		}

		public static void SwapFlyout (string oldFlyout, string flyoutName, object context)
		{
			if (oldFlyout is null)
				throw new ArgumentNullException (nameof (oldFlyout));

			PushFlyout (flyoutName, context);
			PopFlyout (oldFlyout);
		}

		private static Queue<FlyoutHandle> QueuedFlyouts = new Queue<FlyoutHandle> ();
		private static readonly List<Flyout> Flyouts = new List<Flyout> ();
		private static Flyout CurrentFlyout;
		private static FrameworkElement TargetElement;
		private static CoreApplicationViewTitleBar titleBar;
		private static double FlyoutInset = 30;

		private class FlyoutHandle
			: IDisposable
		{
			public FlyoutHandle (string name, object context)
			{
				Name = name;
				Context = context;
			}

			public string Name { get; }
			public object Context { get; }

			public void Attach (Flyout flyout)
			{
				this.flyout = flyout;
			}

			public void Dispose()
			{
				Flyout original = Interlocked.Exchange (ref this.flyout, null);
				if (original != null)
					original.Hide ();
			}

			private Flyout flyout;
		}

		private static Flyout GetFlyout (FlyoutHandle handle)
		{
			var flyout = (Flyout)App.Current.Resources[handle.Name];
			((FrameworkElement)flyout.Content).DataContext = handle.Context;
			handle.Attach (flyout);

			return flyout;
		}

		private static void PushFlyout (FlyoutHandle handle)
		{
			var oldFlyout = CurrentFlyout;
			Flyouts.Add (oldFlyout);

			var flyout = GetFlyout (handle);
			SwapFlyouts (oldFlyout, flyout);
		}

		private static void SwapFlyouts (Flyout oldFlyout, Flyout newFlyout)
		{
			if (oldFlyout != null) {
				oldFlyout.Closed -= OnFlyoutClosed;
				oldFlyout.Hide ();
				((FrameworkElement)oldFlyout.Content).DataContext = null;
			}

			if (titleBar == null) {
				titleBar = CoreApplication.GetCurrentView ().TitleBar;
				UpdateInset ();
				titleBar.LayoutMetricsChanged += (o, e) => UpdateInset ();
			}

			CurrentFlyout = newFlyout;
			if (newFlyout != null) {
				newFlyout.Closed += OnFlyoutClosed;
				CurrentFlyout.ShowAt (TargetElement, new FlyoutShowOptions {
					Position = new Point (TargetElement.ActualWidth / 2, FlyoutInset + 40),
					ShowMode = FlyoutShowMode.Transient
				});
			}
		}

		private static void UpdateInset() => FlyoutInset = (titleBar.Height > 0) ? titleBar.Height : FlyoutInset;

		private static void CloseFlyout (Flyout flyout)
		{
			Flyout newFlyout = null;
			if (CurrentFlyout == flyout && Flyouts.Count > 0) {
				int i = Flyouts.Count - 1;
				newFlyout = Flyouts[i];
				Flyouts.RemoveAt (i);
			}

			SwapFlyouts (flyout, newFlyout);
		}

		private static void OnFlyoutClosed (object sender, object e)
		{
			SetIsVisible (((Flyout)sender).Content, false);

			var closed = (Flyout)sender;
			CloseFlyout (closed);
		}
	}
}
