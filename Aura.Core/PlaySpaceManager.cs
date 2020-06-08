using System;
using System.Collections.Generic;
using System.Text;
using GalaSoft.MvvmLight.Messaging;

namespace Aura
{
	internal class PlaySpaceManager
		: SingleSelectionManager<PlaySpace>
	{
		public PlaySpaceManager (ISyncService syncService)
			: base (syncService)
		{
		}
	}
}