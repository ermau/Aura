//
// PlaybackEnvironmentElement.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura
{
	internal class PlaybackEnvironmentElement
	{
		public PlaybackEnvironmentElement (EncounterStateElement stateElement, EnvironmentElement element)
		{
			StateElement = stateElement ?? throw new ArgumentNullException (nameof (stateElement));
			Element = element ?? throw new ArgumentNullException (nameof (element));
			IsActive = StateElement.StartsWithState;

			foreach (EnvironmentComponent component in Element.GetComponents()) {
				this.componentStates.Add (new ComponentState {
					Component = component,
					IsActive = stateElement.StartsWithState,
					LastIntensity = 1
				});
			}
		}

		public EncounterStateElement StateElement { get; }
		public EnvironmentElement Element { get; }

		public bool IsActive
		{
			get;
			set;
		}

		public double Intensity
		{
			get;
			set;
		} = 1d;

		public void Tick (ActiveServices services)
		{
			// TODO: Modify to handle a controlling component being the trigger point
			// so that we can determine when to get new positions.

			foreach (ComponentState state in this.componentStates) {
				IEnvironmentService service = services.GetService (state.Component);
				if (service == null)
					continue;

				if (state.NextPrepare == null) {
					state.SetupNext (Element, service, Element.Positioning.GetPosition (random));
					state.Timer = Stopwatch.StartNew ();
				}

				bool updating = state.ShouldUpdate;

				if (Intensity != state.LastIntensity) {
					IPreparedEffect currentEffect;
					if (updating || state.CurrentEffect == null)
						currentEffect = state.NextPrepare.Result;
					else
						currentEffect = state.CurrentEffect;

					service.SetIntensity (currentEffect, Intensity);
					state.LastIntensity = Intensity;
				}

				if (updating && state.IsActive) {
					if (state.NextTime == default) {
						state.IsActive = false;
						continue;
					}

					state.CurrentDescriptor = state.NextDescriptor;
					service.PlayEffect (state.NextPrepare.Result);
					state.Timer = Stopwatch.StartNew ();
					state.SetupNext (Element, service, Element.Positioning.GetPosition (random));
				}
			}
		}

		private List<ComponentState> componentStates = new List<ComponentState> ();
		private Random random = new Random ();		

		private class ComponentState
		{
			public EnvironmentComponent Component;
			public TimeSpan? NextTime;
			
			public IPreparedEffect CurrentEffect;
			public string CurrentDescriptor;

			public double LastIntensity;

			public Task<IPreparedEffect> NextPrepare;
			public string NextDescriptor;

			public bool IsActive;
			public Stopwatch Timer;

			public bool ShouldUpdate
			{
				get => NextTime.HasValue && (Timer.Elapsed >= NextTime + (CurrentEffect?.Duration ?? default));
			}

			public bool IsFirst => NextPrepare == null;

			public void SetupNext (EnvironmentElement element, IEnvironmentService service, Position nextPosition)
			{
				bool isFirst = IsFirst;
				if (!isFirst && Component.Playlist.Repeat == SourceRepeatMode.None) {
					NextTime = default;
					return;
				}

				string currentDescriptor = NextDescriptor;
				NextDescriptor = GetNextDescriptor ();
				NextTime = (Component.Timing ?? element.Timing).GetNextTime (this.random, isFirst);

				var options = new PlaybackOptions {
					Position = nextPosition
				};

				if (currentDescriptor == NextDescriptor) {
					var left = CurrentEffect;
					var right = NextPrepare?.Result;

					CurrentEffect = right;
					if (left != null)
						NextPrepare = Task.FromResult (left);
					else
						NextPrepare = service.PrepareEffectAsync (element, NextDescriptor, options);

					return;
				} else
					CurrentEffect = NextPrepare?.Result;

				// TODO: Cache preparations and add adjust playback to engine
				NextPrepare = service.PrepareEffectAsync (element, NextDescriptor, options);
				Trace.WriteLine ($"{element.Name} next effect: {NextDescriptor} in {NextTime}");
			}

			private string GetNextDescriptor ()
			{
				var playlist = Component.Playlist;
				if (playlist.Descriptors.Count == 1)
					return playlist.Descriptors[0];

				int now;
				int last = (CurrentDescriptor != null) ? IndexOf (playlist.Descriptors, CurrentDescriptor) : -1;
				if (playlist.Order == SourceOrder.InOrder) {
					now = last + 1;
					if (now >= playlist.Descriptors.Count)
						now = 0;
				} else {
					do {
						now = random.Next (0, playlist.Descriptors.Count);
					} while (now == last);
				}

				return playlist.Descriptors[now];
			}

			private readonly Random random = new Random ();

			private int IndexOf<T> (IReadOnlyList<T> list, T element)
			{
				for (int i = 0; i < list.Count; i++) {
					if (Equals (list[i], element))
						return i;
				}

				return -1;
			}
		}
	}
}
