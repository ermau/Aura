using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Aura
{
	[TemplatePart (Name = "canvas", Type = typeof(Canvas))]
	internal class RoomLightingView
		: Control
	{
		public RoomLightingView ()
		{
			DefaultStyleKey = typeof (RoomLightingView);
			SizeChanged += OnSizeChanged;
		}

		public static readonly DependencyProperty RoomLightsProperty = DependencyProperty.Register (
			nameof (RoomLights), typeof (IEnumerable<Light>), typeof (RoomLightingView), new PropertyMetadata (default, (o, e) => ((RoomLightingView)o).UpdateLights (e.OldValue, e.NewValue)));

		public IEnumerable<Light> RoomLights
		{
			get => (IEnumerable<Light>)GetValue (RoomLightsProperty);
			set => SetValue (RoomLightsProperty, value);
		}

		protected override void OnApplyTemplate ()
		{
			base.OnApplyTemplate ();
			this.canvas = GetTemplateChild ("canvas") as Canvas;
			if (this.canvas == null)
				throw new InvalidOperationException ($"{nameof (RoomLightingView)} needs a Canvas child named canvas");

			UpdateLights ();
		}

		private Canvas canvas;
		private readonly List<FontIcon> lights = new List<FontIcon> ();

		private void OnSizeChanged (object sender, SizeChangedEventArgs e)
		{
			UpdateLights ();
		}

		private void UpdateLights (object oldLights, object newLights)
		{
			if (oldLights is INotifyCollectionChanged incc)
				incc.CollectionChanged -= OnLightsChanged;
			if (newLights is INotifyCollectionChanged newIncc)
				newIncc.CollectionChanged += OnLightsChanged;

			UpdateLights ();
		}

		private void OnLightsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateLights ();
		}

		private void UpdateLights ()
		{
			if (this.canvas == null)
				return;

			Stack<FontIcon> existing = new Stack<FontIcon> (this.lights);
			this.lights.Clear ();

			if (RoomLights != null) {
				foreach (Light light in RoomLights) {
					if (!existing.TryPop (out FontIcon icon)) {
						icon = new FontIcon {
							Glyph = "\xEA80",
							Width = 24,
							Height = 24,
						};

						this.canvas.Children.Add (icon);
					}

					this.lights.Add (icon);
					if (light.Position == null) {
						Canvas.SetLeft (icon, 0);
						Canvas.SetTop (icon, 0);
						continue;
					}

					double left = (light.Position.X + 1) * (this.canvas.ActualWidth / 2);
					double leftOverhang = this.canvas.ActualWidth - left - icon.Width;
					if (leftOverhang < 0)
						left += leftOverhang;

					double top = (-light.Position.Y + 1) * (this.canvas.ActualHeight / 2);
					double topOverhang = this.canvas.ActualHeight - top - icon.Height;
					if (topOverhang < 0)
						top += topOverhang;

					Canvas.SetLeft (icon, left);
					Canvas.SetTop (icon, top);
				}
			}

			foreach (FontIcon remaining in existing) {
				this.canvas.Children.Remove (remaining);
			}
		}
	}
}
