﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Aura.Data;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EncountersViewModel
		: CampaignElementsViewModel<EncounterElement, EncounterViewModel>
	{
		public EncountersViewModel (IAsyncServiceProvider services)
			: base (services)
		{
		}

		protected override EncounterViewModel InitializeElementViewModel (EncounterElement element)
		{
			return new EncounterViewModel (ServiceProvider, SyncService, element);
		}

		protected override EncounterElement InitializeElement (string name)
		{
			return new EncounterElement {
				Name = name,
				CampaignId = CampaignManager.SelectedElement.Id
			};
		}
	}

	internal class EncounterViewModel
		: ElementViewModel<EncounterElement>
	{
		public EncounterViewModel (IAsyncServiceProvider services, ISyncService sync, EncounterElement element)
			: base (services, sync, element)
		{
		}
	}
}
