using System;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace HEMA
{
	public partial class MainPage : ContentPage
	{
		public Fight Fight { get; }

		public FightSettings Settings { get; }

		public MainPage()
		{
			InitializeComponent();
			Settings = new FightSettings();
			Fight = new Fight(Settings);
			BindingContext = this;
		}

		private void StartTimer(object sender, EventArgs e)
		{
			if (Fight.IsTimerStarted)
			{
				Fight.StopTimer();
				StartBtn.Image.File = "start.png";
			}
			else
			{
				Fight.StartTimer();
				StartBtn.Image.File = "pause.png";
			}
		}

		private void ResetTimer(object sender, EventArgs e)
		{
			Fight.Reset();
		}

		private void DecreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted && Fight.BlueScore > 0)
			{
				Fight.BlueScore--;
			}
		}

		private void DecreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted && Fight.RedScore > 0)
			{
				Fight.RedScore--;
			}
		}

		private void IncreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted)
			{
				Fight.BlueScore++;
			}
		}

		private void IncreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted)
			{
				Fight.RedScore++;
			}
		}

		private void DecreaseDoubleHits(object sender, EventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted && Fight.DoubleHits > 0)
			{
				Fight.DoubleHits--;
			}
		}

		private void IncreaseDoubleHits(object sender, EventArgs e)
		{
			if (!Fight.IsTimerStarted && Fight.IsFightStarted)
			{
				Fight.DoubleHits++;
			}
		}

		private void DecreaseRedViolations(object sender, EventArgs e)
		{
			if (Fight.RedViolations > 0)
			{
				Fight.RedViolations--;
			}
		}

		private void IncreaseRedViolations(object sender, EventArgs e)
		{
			Fight.RedViolations++;
		}

		private void DecreaseBlueViolations(object sender, EventArgs e)
		{
			if (Fight.BlueViolations > 0)
			{
				Fight.BlueViolations--;
			}
		}

		private void IncreaseBlueViolations(object sender, EventArgs e)
		{
			Fight.BlueViolations++;
		}
	}
}
