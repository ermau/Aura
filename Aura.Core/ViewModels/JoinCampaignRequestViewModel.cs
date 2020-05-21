using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Aura.Messages;
using Aura.Service;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class JoinCampaignRequestViewModel
		: ViewModelBase
	{
		public JoinCampaignRequestViewModel (RemoteCampaign campaign)
		{
			Campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
			JoinCommand = new RelayCommand (() => MessengerInstance.Send (new RequestJoinCampaignMessage (Campaign)));
		}

		public RemoteCampaign Campaign { get; }

		public ICommand JoinCommand
		{
			get;
		}

		public string Message => $"Would you like to join {Campaign.Name}?";
	}
}
