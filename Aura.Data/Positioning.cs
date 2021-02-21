//
// Positioning.cs
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

namespace Aura.Data
{
	public record Positioning
	{
		public Position FixedPosition
		{
			get;
			init;
		}

		public Position MinimumDistance
		{
			get;
			init;
		}

		public Position MaximumDistance
		{
			get;
			init;
		}

		public bool HeightRelevant
		{
			get;
			init;
		} = true;

		// TODO:
		//  - toggle position from below
		//  - Cone facing
		//  - Random offset from fixed position

		public Position GetPosition (Random rand)
		{
			if (FixedPosition != null)
				return FixedPosition;

			Position min = MinimumDistance ?? new Position ();
			Position max = MaximumDistance ?? new Position { X = 2, Y = 2, Z = 2 };

			int fbx = (rand.Next (2) == 0) ? -1 : 1;
			int fby = (rand.Next (2) == 0) ? -1 : 1;
			int fbz = (rand.Next (2) == 0) ? -1 : 1;

			float x = (float)((min.X >= max.X) ? min.X : rand.NextDouble() * (max.X - min.Y)) * fbx;
			float y = (float)((min.Y >= max.Y) ? min.Y : rand.NextDouble() * (max.Y - min.Y)) * fby;
			//float z = (HeightRelevant) ? ((min.Z >= max.Z) ? min.Z : rand.Next (min.Z, max.Z) * fbz) : 0;

			return new Position {
				X = x,
				Y = y,
				//Z = z
			};
		}
	}
}
