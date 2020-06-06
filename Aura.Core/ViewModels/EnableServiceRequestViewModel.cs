using System;
using System.Windows.Input;
using Aura.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EnableServiceRequestViewModel
		: PromptRequestViewModel
	{
		public EnableServiceRequestViewModel (IService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
			EnableCommand = new RelayCommand (() => {
				MessengerInstance.Send (new EnableServiceMessage (Service));
				IsOpen = false;
			});
		}

		public override string Message
		{
			get => $"Enable {Service.DisplayName} for current space?";
		}
		
		public IService Service { get; }

		public ICommand EnableCommand
		{
			get;
		}
	}
}
