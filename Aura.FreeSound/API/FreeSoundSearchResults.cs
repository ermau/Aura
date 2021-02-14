namespace Aura.FreeSound.API
{
	public class FreeSoundSearchResults
		: FreeSoundPagedResponse<FreeSoundInstance>
	{
		public IReadOnyList<string> Fields
		{
			get;
			set;
		}
	}
}
