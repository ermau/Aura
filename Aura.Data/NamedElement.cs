namespace Aura.Data
{
	public record NamedElement
		: Element
	{
		public string Name
		{
			get;
			init;
		}
	}
}