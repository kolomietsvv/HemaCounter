using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace HEMA
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			var mainPage = new MainPage();

			MainPage = new NavigationPage(mainPage);
			//MainPage.Navigation.PushAsync(new CommonSettingsPage());
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
