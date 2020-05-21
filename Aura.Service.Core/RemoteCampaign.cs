using System;

namespace Aura.Service
{
	public class RemoteCampaign
    {
        public Guid id
        {
            get;
            set;
        }

        public Guid Secret
        {
            get;
            set;
        }

        public string Part
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }
    }
}
