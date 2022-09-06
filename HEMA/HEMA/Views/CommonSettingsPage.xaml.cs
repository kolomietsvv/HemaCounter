using HEMA.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HEMA
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CommonSettingsPage : ContentPage
	{
		public CommonSettingsPage()
		{
			InitializeComponent();
			BindingContext = App.Current.MainPage;
		}

		private void RemoveExtraCharacters(object sender, TextChangedEventArgs e)
			=> TextHelper.RemoveExtraCharacters(sender, e);
    }
}