using Android.Media;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace HEMA
{
	public partial class App : Application
	{
		public App(MediaPlayer tickMediaPlayer, MediaPlayer alarmMediaPlayer, MediaPlayer alarmPauseMediaPlayer)
		{
			InitializeComponent();
			MainPage = new NavigationPage(new MainPage(tickMediaPlayer, alarmMediaPlayer, alarmPauseMediaPlayer));
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
