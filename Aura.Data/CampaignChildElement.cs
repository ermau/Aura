namespace Aura.Data
{
	public record CampaignChildElement
		: NamedElement
	{
		[ElementId]
		public string CampaignId
		{
			get;
			init;
		}
	}
}
