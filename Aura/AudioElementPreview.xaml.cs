using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace Aura
{
	public sealed partial class AudioElementPreview : UserControl
	{
		public AudioElementPreview ()
		{
			InitializeComponent ();
			SizeChanged += (o, e) => OnIntensityChanged ();
		}

		public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register (nameof (Intensity), typeof (double), typeof (AudioElementPreview),
			new PropertyMetadata (0, (dobj, e) => ((AudioElementPreview)dobj).OnIntensityChanged()));

		public double Intensity
		{
			get => (double)GetValue (IntensityProperty);
			set => SetValue (IntensityProperty, value);
		}

		private void OnIntensityChanged()
		{
			double i = Intensity;
			//SetScale (this.originScale, (i > 0) ? 1 : 0);
			SetScale (this.innerScale, i);
			SetScale (this.outerScale, i);
		}

		private void SetScale (ScaleTransform transform, double scale)
		{
			transform.ScaleX = scale;
			transform.ScaleY = scale;
		}
	}
}
