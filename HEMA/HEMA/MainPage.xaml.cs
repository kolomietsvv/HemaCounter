using System;
using System.Diagnostics;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace HEMA
{
	public partial class MainPage : ContentPage
	{
		public bool IsTimerStarted { get; set; }
		public Timer Timer { get; set; }
		public Stopwatch Stopwatch { get; set; }
		public int BlueScore { get; set; }
		public int RedScore { get; set; }
		public int DoubleHits { get; set; }
		public int RedPreventions { get; set; }
		public int BluePreventions { get; set; }

		public MainPage()
		{
			InitializeComponent();
			Stopwatch = new Stopwatch();
		}

		private void StartTimer(object sender, EventArgs e)
		{
			if (IsTimerStarted)
			{
				Timer.Change(0, Timeout.Infinite);
				Stopwatch.Stop();
				StartBtn.Image.File = "start.png";
			}
			else
			{
				if (Timer == null)
					Timer = new Timer(TimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
				else
					Timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
				Stopwatch.Start();
				StartBtn.Image.File = "pause.png";
			}
			IsTimerStarted = !IsTimerStarted;
		}

		private void ResetTimer(object sender, EventArgs e)
		{
			if (Timer != null)
			{
				Timer.Change(0, Timeout.Infinite);
			}

			Stopwatch.Reset();
			IsTimerStarted = false;
			BlueScore = 0;
			RedScore = 0;
			DoubleHits = 0;
			RedPreventions = 0;
			BluePreventions = 0;
			DoubleHitsLbl.Text = DoubleHits.ToString();
			RedPreventionsLbl.Text = RedPreventions.ToString();
			BluePreventionsLbl.Text = BluePreventions.ToString();
			BlueScoreLbl.Text = BlueScore.ToString();
			RedScoreLbl.Text = RedScore.ToString();
			TimerLbl.Text = Stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
			StartBtn.Image.File = "start.png";
		}

		private void DecreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero && BlueScore > 0)
			{
				BlueScore--;
				BlueScoreLbl.Text = BlueScore.ToString();
			}
		}

		private void DecreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero && RedScore > 0)
			{
				RedScore--;
				RedScoreLbl.Text = RedScore.ToString();
			}
		}

		private void IncreaseBlueScore(object sender, SwipedEventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero)
			{
				BlueScore++;
				BlueScoreLbl.Text = BlueScore.ToString();
			}
		}

		private void IncreaseRedScore(object sender, SwipedEventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero)
			{
				RedScore++;
				RedScoreLbl.Text = RedScore.ToString();
			}
		}

		private void DecreaseDoubleHits(object sender, EventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero && DoubleHits > 0)
			{
				DoubleHits--;
				DoubleHitsLbl.Text = DoubleHits.ToString();
			}
		}

		private void IncreaseDoubleHits(object sender, EventArgs e)
		{
			if (!IsTimerStarted && Stopwatch.Elapsed != TimeSpan.Zero)
			{
				DoubleHits++;
				DoubleHitsLbl.Text = DoubleHits.ToString();
			}
		}

		private void DecreaseRedPreventions(object sender, EventArgs e)
		{
			if (RedPreventions > 0)
			{
				RedPreventions--;
				RedPreventionsLbl.Text = RedPreventions.ToString();
			}
		}

		private void IncreaseRedPreventions(object sender, EventArgs e)
		{
			RedPreventions++;
			RedPreventionsLbl.Text = RedPreventions.ToString();
		}

		private void DecreaseBluePreventions(object sender, EventArgs e)
		{
			if (BluePreventions > 0)
			{
				BluePreventions--;
				BluePreventionsLbl.Text = BluePreventions.ToString();
			}
		}

		private void IncreaseBluePreventions(object sender, EventArgs e)
		{
			BluePreventions++;
			BluePreventionsLbl.Text = BluePreventions.ToString();
		}

		private void TimerElapsed(object state)
		{
			MainThread.BeginInvokeOnMainThread(() => TimerLbl.Text = Stopwatch.Elapsed.ToString(@"hh\:mm\:ss"));
		}
	}
}
