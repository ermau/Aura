//
// PlaybackEnvironment.cs
//
// Authors:
//       Eric Maupin <me@ermau.com>
//
// Copyright (c) 2021 Eric Maupin
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aura
{

	internal class PlaybackEnvironment
	{
		public PlaybackEnvironment (IReadOnlyCollection<PlaybackEnvironmentElement> elements)
		{
			if (elements is null)
				throw new ArgumentNullException (nameof (elements));

			this.elements = elements.ToArray();
		}

		public void Tick (ActiveServices services)
		{
			foreach (PlaybackEnvironmentElement element in this.elements) {
				if (!element.IsActive)
					continue;

				if (this.transitions.TryGetValue (element, out Transition transition)) {
					element.Intensity = transition.GetIntensity ();
					if (transition.IsFinished) {
						this.transitions.TryRemove (element, out Transition v);
						if (transition.EndingIntensity <= 0)
							element.IsActive = false;
					}
				}

				element.Tick (services);
			}
		}

		public Task TransitionToAsync (PlaybackEnvironment environment, TimeSpan overTime)
		{
			var tasks = new List<Task> ();
			foreach (PlaybackEnvironmentElement element in this.elements) {
				Transition transition = new Transition {
					Length = overTime,
					StartingIntensity = element.Intensity
				};

				tasks.Add (transition.Task);

				bool fadeOut = true;
				// TODO: Keep same elements playing as-is

				if (fadeOut) {
					transition.EndingIntensity = 0;
				}

				this.transitions[element] = transition;
			}

			return Task.WhenAll (tasks);
		}

		public Task FadeInAsync (TimeSpan overTime)
		{
			var tasks = new List<Task> ();
			foreach (PlaybackEnvironmentElement element in this.elements) {
				double ending = element.Intensity;
				element.IsActive = element.StateElement.StartsWithState;
				element.Intensity = 0;

				var transition = new Transition {
					StartingIntensity = 0,
					EndingIntensity = ending,
					Length = overTime,
				};

				tasks.Add (transition.Task);
				this.transitions[element] = transition;
			}

			return Task.WhenAll (tasks);
		}

		public Task FadeOutAsync (TimeSpan overTime)
		{
			return TransitionToAsync (null, overTime);
		}

		public void Stop()
		{
			foreach (PlaybackEnvironmentElement element in this.elements) {
				element.IsActive = false;
				this.transitions.TryRemove (element, out _);
			}
		}

		private readonly Random random = new Random ();
		private readonly PlaybackEnvironmentElement[] elements;
		private readonly ConcurrentDictionary<PlaybackEnvironmentElement, Transition> transitions = new ConcurrentDictionary<PlaybackEnvironmentElement, Transition> ();
	}
}
