namespace Aura.Data
{
	public record CampaignChildElement
		: NamedElement
	{
		public string CampaignId
		{
			get;
			init;
		}
	}
}
