using System;

namespace Aura.Service
{
	public class Campaign
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
