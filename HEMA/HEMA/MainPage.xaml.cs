using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Input;
using Xamarin.Forms;

namespace HEMA
{
	public partial class MainPage : ContentPage
	{
		private string settingsPath;
		private CommonSettingsPage commonSettingsPage;

		private UserDeclines userDeclines;

		public Fight Fight { get; }

		public ICommand ResetSettingsCmd { get => new Command(Fight.Settings.SetDefaults); }

		public MainPage()
		{
			InitializeComponent();
			settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.json");
			FightSettings settings = GetFightSettings();
			userDeclines = new UserDeclines();
			Fight = new Fight(settings);
			Fight.IsOneDoubleHitLeft += isOneDoubleHitLeft => DoubleHitlLbl.TextColor = isOneDoubleHitLeft ? Color.Red : Color.Default;
			Fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog("Максимальное число обоюдных поражений", FinishCause.DoubleHits);
			Fight.MaxScoreReached += () => DisplayFinishFightDialog("Максимальне число очков", FinishCause.MaxScore);
			Fight.TimeAlert += () => DisplayFinishFightDialog("Время вышло", FinishCause.Time);
			BindingContext = this;
			commonSettingsPage = new CommonSettingsPage();
			commonSettingsPage.BindingContext = this;
		}

		public void OpenSettingsTab(object sender, EventArgs e)
		{
			Navigation.PushAsync(commonSettingsPage);
		}

		private void StartTimer(object sender, EventArgs e)
		{
			if (Fight.IsTimerStarted)
			{
				Fight.StopTimer();
				StartBtn.TextColor = Color.LightSlateGray;
			}
			else
			{
				Fight.StartTimer();
				StartBtn.TextColor = Color.Default;
			}
		}

		private void ResetTimer(object sender, EventArgs e)
		{
			Fight.Reset();
			StartBtn.TextColor = Color.LightSlateGray;
			userDeclines.Reset();
		}

		private void DecreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (Fight.IsScoreChangeEnabled && Fight.BlueScore > 0)
			{
				Fight.BlueScore--;
			}
		}

		private void DecreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (Fight.IsScoreChangeEnabled && Fight.RedScore > 0)
			{
				Fight.RedScore--;
			}
		}

		private void IncreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.BlueScore++;
			}
		}

		private void IncreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.RedScore++;
			}
		}

		private void DecreaseDoubleHits(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled && Fight.DoubleHits > 0)
			{
				Fight.DoubleHits--;
			}
		}

		private void IncreaseDoubleHits(object sender, EventArgs e)
		{
			Fight.DoubleHits++;
		}

		private void DecreaseRedViolations(object sender, EventArgs e)
		{
			Fight.RedViolations--;
		}

		private void IncreaseRedViolations(object sender, EventArgs e)
		{
			Fight.RedViolations++;
		}

		private void DecreaseBlueViolations(object sender, EventArgs e)
		{
			Fight.BlueViolations--;
		}

		private void IncreaseBlueViolations(object sender, EventArgs e)
		{
			Fight.BlueViolations++;
		}

		private async void DisplayFinishFightDialog(string cause, FinishCause finishCause)
		{
			switch (finishCause)
			{
				case FinishCause.DoubleHits:
					if (userDeclines.UserDeclinedDoubleHitsFinish)
						return;
					break;
				case FinishCause.MaxScore:
					if (userDeclines.UserDeclinedMaxScoreFinish)
						return;
					break;
				case FinishCause.Time:
					if (userDeclines.UserDeclinedTimeFinish)
						return;
					break;
			}

			Fight.StopTimer();
			var userChoice = await DisplayAlert($"Бой окончен", cause, "Завершить", "Продолжить");
			if (userChoice)
				Fight.Reset();

			else
				switch (finishCause)
				{
					case FinishCause.DoubleHits:
						userDeclines.UserDeclinedDoubleHitsFinish = true;
						break;
					case FinishCause.MaxScore:
						userDeclines.UserDeclinedMaxScoreFinish = true;
						break;
					case FinishCause.Time:
						userDeclines.UserDeclinedTimeFinish = true;
						break;
				}
		}

		private FightSettings GetFightSettings()
		{
			FightSettings settings;
			if (File.Exists(settingsPath))
			{
				var settingsString = File.ReadAllText(settingsPath);
				settings = JsonConvert.DeserializeObject<FightSettings>(settingsString);
			}
			else
			{
				settings = new FightSettings();
			}

			return settings;
		}

		protected override void OnAppearing()
		{
			var settingsString = JsonConvert.SerializeObject(Fight.Settings);
			File.WriteAllText(settingsPath, settingsString);
			base.OnAppearing();
		}
	}

	struct UserDeclines
	{
		public bool UserDeclinedDoubleHitsFinish;
		public bool UserDeclinedMaxScoreFinish;
		public bool UserDeclinedTimeFinish;

		public void Reset()
		{
			UserDeclinedDoubleHitsFinish = false;
			UserDeclinedMaxScoreFinish = false;
			UserDeclinedTimeFinish = false;
		}
	}

	enum FinishCause
	{
		DoubleHits = 1,
		MaxScore = 2,
		Time = 3,
	}
}
