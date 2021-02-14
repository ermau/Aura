using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Aura.Data
{
	public record FileSample
		: NamedElement
	{
		public string ContentHash
		{
			get;
			init;
		}

		public string SourceUrl
		{
			get;
			init;
		}

		public ContentAuthor Author
		{
			get;
			init;
		}

		public ContentLicense License
		{
			get;
			init;
		}

		public TimeSpan Duration
		{
			get;
			init;
		}

		/// <summary>
		/// Gets a list of event points in the sample
		/// </summary>
		/// <remarks>
		/// <para>
		/// An example sample might be a long running rain & thunder track. In such a
		/// sample track, event points can be used to trigger lighting effects for when
		/// the thunder occurs in the track.
		/// </para>
		/// <para>
		/// A video example might be an explosion on screen that triggers lighting or other
		/// effects. Perhaps the video switches to a foggy scene and a controlled fogger begins
		/// to emit fog.
		/// </para>
		/// </remarks>
		[DataMember (EmitDefaultValue = false)]
		public IReadOnlyList<SampleEventPoint> Events
		{
			get;
			init;
		}
	}

	public record SampleEventPoint
	{
		public int EventType
		{
			get;
			init;
		}

		public TimeSpan Time
		{
			get;
			init;
		}
	}
}
