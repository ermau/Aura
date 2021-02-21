namespace Aura.Data
{
	public record LightingConfiguration
	{
		public string Service
		{
			get;
			init;
		}

		public string PairedDevice
		{
			get;
			init;
		}

		public string RoomId
		{
			get;
			init;
		}
	}
}
