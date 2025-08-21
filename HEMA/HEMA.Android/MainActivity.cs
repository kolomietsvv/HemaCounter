using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;

namespace HEMA.Droid
{
	[Activity(Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(savedInstanceState);
			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
			var tickAudioPlayer = MediaPlayer.Create(Application.Context, Resource.Raw.tick);
			var alarmAudioPlayer = MediaPlayer.Create(Application.Context, Resource.Raw.beep);
			var alarmPauseAudioPlayer = MediaPlayer.Create(Application.Context, Resource.Raw.longBeep);
			LoadApplication(new App(tickAudioPlayer, alarmAudioPlayer, alarmPauseAudioPlayer));
		}
	}
}