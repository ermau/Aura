//
// Transition.cs
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
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aura
{
	internal class Transition
	{
		public double StartingIntensity;
		public double EndingIntensity;
		public Stopwatch Timer = Stopwatch.StartNew();
		public TimeSpan Length;
		public Easing Easing = Easing.CubicOut;

		public Task Task => this.tcs.Task;

		public bool IsFinished => Timer.Elapsed >= Length;

		public double GetIntensity ()
		{
			double timed = Timer.Elapsed.TotalMilliseconds;
			if (timed >= Length.TotalMilliseconds) {
				this.tcs.TrySetResult (true);
				return EndingIntensity;
			}

			double ease = Easing.Ease ((double)timed / Length.TotalMilliseconds);
			return StartingIntensity + ((EndingIntensity - StartingIntensity) * ease);
		}

		private readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
	}
}
