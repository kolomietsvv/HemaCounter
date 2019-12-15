using Android.Media;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace HEMA
{
	public partial class App : Application
	{
		public App(MediaPlayer mediaPlayer)
		{
			InitializeComponent();
			MainPage = new NavigationPage(new MainPage(mediaPlayer));
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
