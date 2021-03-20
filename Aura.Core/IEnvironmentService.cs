//
// IEnvironmentalService.cs
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
using System.Reflection;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura
{
	public interface IPreparedEffect
		: IDisposable
	{
		TimeSpan Duration { get; }
	}

	public class PreparedEffectEventArgs
		: EventArgs
	{
		public PreparedEffectEventArgs (IPreparedEffect effect)
		{
			Effect = effect ?? throw new ArgumentNullException (nameof (effect));
		}

		public IPreparedEffect Effect { get; }
	}

	public interface ISampledService
		: IEnvironmentService
	{
		Task<FileSample> ScanSampleAsync (FileSample sample, IProgress<double> progress = null);
	}

	public interface IEnvironmentService
		: IService
	{
		Task StartAsync (IAsyncServiceProvider services);
		Task StopAsync ();

		Task<IPreparedEffect> PrepareEffectAsync (EnvironmentElement element, string descriptor, PlaybackOptions options);
		void PlayEffect (IPreparedEffect effect);
		void SetIntensity (IPreparedEffect element, double intensity);

		/*
		event EventHandler<PreparedElementEventArgs> ElementFinished;

		Task<IPreparedElement> PrepareElementAsync (EnvironmentElement element, PlaybackOptions options, CancellationToken cancellationToken);

		Task PlayElementAsync (IPreparedElement element);
		Task SetIntensityAsync (IPreparedElement element, double intensity);
		Task AdjustPlaybackAsync (IPreparedElement element, PlaybackOptions options);

		Task StartAsync (IAsyncServiceProvider services);
		Task StopAsync ();*/
	}

	public static class EnvironmentServices
	{
		public static async Task<ISampledService> GetServiceAsync (this IAsyncServiceProvider self, FileSample sample)
		{
			if (self is null)
				throw new ArgumentNullException (nameof (self));
			if (sample is null)
				throw new ArgumentNullException (nameof (sample));

			if (!ServiceMapping.TryGetValue (sample.GetType (), out Type serviceType))
				return null;

			PlaySpaceManager playSpace = await self.GetServiceAsync<PlaySpaceManager> ().ConfigureAwait (false);
			await playSpace.Loading.ConfigureAwait (false);

			IEnumerable<IEnvironmentService> services = await self.GetServicesAsync<IEnvironmentService> ().ConfigureAwait (false);
			services = services.Where (s => serviceType.IsAssignableFrom (s.GetType ()));

			PlaySpaceElement space = playSpace.SelectedElement;
			if (space != null) {
				services = services.Where (s => space.Services.Contains (s.GetType ().GetSimpleTypeName ()));
			}

			return (ISampledService)services.FirstOrDefault ();
		}

		private static readonly Dictionary<Type, Type> ServiceMapping = new Dictionary<Type, Type> {
			{ typeof(AudioSample), typeof(IAudioService) }
		};
	}
}
