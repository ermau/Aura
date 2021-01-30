using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;
using GalaSoft.MvvmLight;

namespace Aura.ViewModels
{
	internal class PlaySpacesViewModel
		: EditSingleSelectionItemsViewModel<PlaySpaceElement, EditSingleSelectionElementViewModel<PlaySpaceElement>>
	{
		public PlaySpacesViewModel(IAsyncServiceProvider services)
			: base (services)
		{
		}

		protected override EditSingleSelectionElementViewModel<PlaySpaceElement> CreateViewModel (IAsyncServiceProvider services, PlaySpaceElement element)
		{
			return new EditPlaySpaceViewModel (services, element);
		}

		protected override async Task<SingleSelectionManager<PlaySpaceElement>> GetManagerAsync ()
		{
			return await Services.GetServiceAsync<PlaySpaceManager> ();
		}
	}

	internal class EditPlaySpaceViewModel
		: EditSingleSelectionElementViewModel<PlaySpaceElement>
	{
		public EditPlaySpaceViewModel (IAsyncServiceProvider services, PlaySpaceElement element)
			: base (services, element)
		{
		}
	}
}
