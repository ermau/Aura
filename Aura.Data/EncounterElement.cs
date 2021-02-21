using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record EncounterElement
		: CampaignChildElement
	{
		public IReadOnlyList<EncounterState> States
		{
			get;
			init;
		} = Array.Empty<EncounterState> ();
	}

	public record EncounterState
	{
		public string Name
		{
			get;
			init;
		}

		public IReadOnlyList<EncounterStateElement> EnvironmentElements
		{
			get;
			init;
		} = Array.Empty<EncounterStateElement> ();
	}

	public record EncounterStateElement
	{
		public string ElementId
		{
			get;
			init;
		}

		public bool StartsWithState
		{
			get;
			init;
		} = true;
	}
}
