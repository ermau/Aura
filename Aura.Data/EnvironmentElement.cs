using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record EnvironmentElement
		: CampaignChildElement
	{
		public Timing Timing
		{
			get;
			init;
		} = new Timing ();

		public Positioning Positioning
		{
			get;
			init;
		} = new Positioning ();

		public AudioComponent Audio
		{
			get;
			init;
		}

		public LightingComponent Lighting
		{
			get;
			init;
		}
	}

	public record EnvironmentComponent
	{
		public Timing Timing
		{
			get;
			init;
		}
	}

	public record PositionedComponent
		: EnvironmentComponent
	{
		public Positioning Positioning
		{
			get;
			init;
		}
	}

	public record AudioComponent
		: EnvironmentComponent
	{
		public ElementPlaylist Playlist
		{
			get;
			init;
		} = new ElementPlaylist ();
	}

	public record LightingComponent
		:  PositionedComponent
	{
		public LightingEffect Effect
		{
			get;
			init;
		}
	}
}