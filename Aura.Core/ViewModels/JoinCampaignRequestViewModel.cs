﻿using System;
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
		: PromptRequestViewModel
	{
		public JoinCampaignRequestViewModel (RemoteCampaign campaign)
		{
			Campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
			JoinCommand = new RelayCommand (() => {
				MessengerInstance.Send (new JoinCampaignMessage (Campaign));
				IsOpen = false;
			});
		}

		public RemoteCampaign Campaign { get; }

		public ICommand JoinCommand
		{
			get;
		}

		public override string Message
		{
			get => $"Would you like to join {Campaign.Name}?";
		}
	}

	internal class ConnectCampaignRequestViewModel
			: PromptRequestViewModel
	{
		public ConnectCampaignRequestViewModel (RemoteCampaign campaign)
		{
			Campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
			JoinCommand = new RelayCommand (() => {
				MessengerInstance.Send (new JoinConnectCampaignMessage(Campaign));
				IsOpen = false;
			});
		}

		public RemoteCampaign Campaign { get; }

		public ICommand JoinCommand
		{
			get;
		}

		public override string Message
		{
			get => $"Would you like to connect to {Campaign.Name}?";
		}
	}
}
