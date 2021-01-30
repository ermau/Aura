using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Aura.Data;
using Aura.Service;
using Aura.Service.Client;

using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class CreateCampaignDialogViewModel
		: InputDialogViewModel
	{
		public CreateCampaignDialogViewModel (IAsyncServiceProvider serviceProvider)
		{
			CreateCampaign = new RelayCommand (OnCreate, CanCreate);
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException (nameof (serviceProvider));
		}

		public ICommand CreateCampaign
		{
			get;
		}

		public bool IsOnline
		{
			get;
			set;
		}

		protected override RelayCommand Command => (RelayCommand)CreateCampaign;

		private readonly IAsyncServiceProvider serviceProvider;

		private async void OnCreate()
		{
			CampaignElement c;
			IsBusy = true;
			try {
				var manager = await this.serviceProvider.GetServiceAsync<CampaignManager> ();
				if (IsOnline) {
					var serviceClient = await this.serviceProvider.GetServiceAsync<ILiveCampaignClient> ();
					RemoteCampaign rcampaign;
					try {
						rcampaign = await serviceClient.CreateCampaignAsync (Input.Trim (), default);
					} catch (Exception ex) {
						Error = ex.Message;
						return;
					}

					c = await manager.CreateCampaignAsync (rcampaign);
				} else {
					var sync = await this.serviceProvider.GetServiceAsync<ISyncService> ();

					var campaign = new CampaignElement {
						Name = Input.Trim (),
						IsRemote = false,
					};
					c = await sync.SaveElementAsync (campaign);
				}

				manager.SelectedElement = c;
			} finally {
				IsBusy = false;
			}
		}

		private bool CanCreate () => !String.IsNullOrWhiteSpace (Input);
	}
}
