using System;
using System.Windows.Input;
using Aura.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EnableServiceRequestViewModel
		: ViewModelBase
	{
		public EnableServiceRequestViewModel (IService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
			EnableCommand = new RelayCommand (() => MessengerInstance.Send (new EnableServiceMessage (Service)));
		}

		public string Message => $"Enable {Service.DisplayName} for current space?";
		
		public IService Service { get; }

		public ICommand EnableCommand
		{
			get;
		}
	}
}
