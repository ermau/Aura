using Aura.ViewModels;

namespace Aura
{
	public class PlaySpacesPage
		: MasterDetailPage
	{
		public PlaySpacesPage()
		{
			Title = "Play Spaces";
			ShowSorting = false;
			DataContext = new PlaySpacesViewModel (App.Services);
			PaneContent = new PlaySpaceEditorView ();
		}
	}
}
