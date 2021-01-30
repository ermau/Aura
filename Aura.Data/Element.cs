namespace Aura.Data
{
	public abstract record Element
	{
		public string Id;
		public int Version;

		public Element Update (string id = null)
		{
			return this with { Id = id ?? Id, Version = Version + 1 };
		}
	}
}