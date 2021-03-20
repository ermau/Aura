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

		public EnvironmentComponent[] GetComponents()
		{
			if (this.components == null)
				this.components = new EnvironmentComponent[] { Audio, Lighting };

			return this.components;
		}

		private EnvironmentComponent[] components;
	}

	public record EnvironmentComponent
	{
		public Timing Timing
		{
			get;
			init;
		}

		public ElementPlaylist Playlist
		{
			get;
			init;
		} = new ElementPlaylist ();
	}

	public record AudioComponent
		: EnvironmentComponent
	{
	}

	public record LightingComponent
		: EnvironmentComponent
	{
	}
}