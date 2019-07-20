using System;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace HEMA
{
	public partial class MainPage : ContentPage
	{
		Fight fight;

		public MainPage()
		{
			InitializeComponent();
			fight = new Fight(TimerElapsed);
		}

		private void StartTimer(object sender, EventArgs e)
		{
			IncreaseDoubleHitsBtn.IsEnabled = fight.IsTimerStarted;
			DecreaseDoubleHitsBtn.IsEnabled = fight.IsTimerStarted;

			if (fight.IsTimerStarted)
			{
				fight.StopTimer();
				StartBtn.Image.File = "start.png";
			}
			else
			{
				fight.StartTimer();
				StartBtn.Image.File = "pause.png";
			}
		}

		private void ResetTimer(object sender, EventArgs e)
		{
			fight.Reset();
			DoubleHitsLbl.Text = fight.DoubleHits.ToString();
			RedViolationsLbl.Text = fight.RedViolations.ToString();
			BlueViolationsLbl.Text = fight.BlueViolations.ToString();
			BlueScoreLbl.Text = fight.BlueScore.ToString();
			RedScoreLbl.Text = fight.RedScore.ToString();
			TimerLbl.Text = fight.Elapsed.ToString(@"hh\:mm\:ss");
			StartBtn.Image.File = "start.png";
		}

		private void DecreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted && fight.BlueScore > 0)
			{
				fight.BlueScore--;
				BlueScoreLbl.Text = fight.BlueScore.ToString();
			}
		}

		private void DecreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted && fight.RedScore > 0)
			{
				fight.RedScore--;
				RedScoreLbl.Text = fight.RedScore.ToString();
			}
		}

		private void IncreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted)
			{
				fight.BlueScore++;
				BlueScoreLbl.Text = fight.BlueScore.ToString();
			}
		}

		private void IncreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted)
			{
				fight.RedScore++;
				RedScoreLbl.Text = fight.RedScore.ToString();
			}
		}

		private void DecreaseDoubleHits(object sender, EventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted && fight.DoubleHits > 0)
			{
				fight.DoubleHits--;
				DoubleHitsLbl.Text = fight.DoubleHits.ToString();
			}
		}

		private void IncreaseDoubleHits(object sender, EventArgs e)
		{
			if (!fight.IsTimerStarted && fight.IsStarted)
			{
				fight.DoubleHits++;
				DoubleHitsLbl.Text = fight.DoubleHits.ToString();
			}
		}

		private void DecreaseRedViolations(object sender, EventArgs e)
		{
			if (fight.RedViolations > 0)
			{
				fight.RedViolations--;
				RedViolationsLbl.Text = fight.RedViolations.ToString();
			}
		}

		private void IncreaseRedViolations(object sender, EventArgs e)
		{
			fight.RedViolations++;
			RedViolationsLbl.Text = fight.RedViolations.ToString();
		}

		private void DecreaseBlueViolations(object sender, EventArgs e)
		{
			if (fight.BlueViolations > 0)
			{
				fight.BlueViolations--;
				BlueViolationsLbl.Text = fight.BlueViolations.ToString();
			}
		}

		private void IncreaseBlueViolations(object sender, EventArgs e)
		{
			fight.BlueViolations++;
			BlueViolationsLbl.Text = fight.BlueViolations.ToString();
		}

		private void TimerElapsed(object state)
		{
			MainThread.BeginInvokeOnMainThread(() => TimerLbl.Text = fight.Elapsed.ToString(@"hh\:mm\:ss"));
		}
	}
}
