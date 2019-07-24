using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HEMA
{
	public partial class Fight : INotifyPropertyChanged
	{
		private Stopwatch stopwatch;
		private Timer timer;
		private Phrase previousPhrase;
		private Phrase currentPhrase;

		private bool isTimerStarted;

		private int doubleHits;
		private int blueViolations;
		private int redViolations;

		public event Action DoubeHitsInRowReached;
		public event Action CommonDoubeHitsReached;
		public event Action MaxScoreReached;
		public event Action TimeAlert;
		public event PropertyChangedEventHandler PropertyChanged;

		public FightSettings Settings { get; }

		public bool IsTimerStarted
		{
			get => isTimerStarted;
			private set
			{
				isTimerStarted = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTimerStarted)));
			}
		}
		public bool IsDoubleHitsInRow { get; private set; }

		public int DoubleHits
		{
			get => doubleHits;
			set
			{
				doubleHits = value;
				if (Settings.UseFightSettings && IsDoubleHitsInRow && doubleHits >= Settings.DoubleHitsInARow)
					DoubeHitsInRowReached?.Invoke();
				else if (Settings.UseFightSettings && !IsDoubleHitsInRow && doubleHits >= Settings.DoubleHitsCommon)
					CommonDoubeHitsReached?.Invoke();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoubleHits)));
			}
		}

		public int BlueViolations
		{
			get => blueViolations;
			set
			{
				blueViolations = value;
				if (Settings.UseFightSettings && value >= Settings.ViolationsCount)
					blueViolations -= Settings.PenaltyPoints;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueViolations)));
			}
		}

		public int RedViolations
		{
			get => redViolations;
			set
			{
				redViolations = value;
				if (Settings.UseFightSettings && value >= Settings.ViolationsCount)
					redViolations -= Settings.PenaltyPoints;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedViolations)));
			}
		}

		public int RedScore
		{
			get => currentPhrase.RedScore;
			set
			{
				currentPhrase.RedScore = value;
				if (Settings.UseFightSettings && value >= Settings.MaxFightScore)
					MaxScoreReached?.Invoke();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedScore)));
			}
		}

		public int BlueScore
		{
			get => currentPhrase.BlueScore;
			set
			{
				currentPhrase.BlueScore = value;
				if (Settings.UseFightSettings && value >= Settings.MaxFightScore)
					MaxScoreReached?.Invoke();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueScore)));
			}
		}

		public TimeSpan Elapsed => stopwatch.Elapsed;
		public bool IsFightStarted => stopwatch.Elapsed != TimeSpan.Zero;

		public Fight(FightSettings settings)
		{
			TimerCallback timerElapsed = null;
			Settings = settings;
			IsDoubleHitsInRow = true;
			stopwatch = new Stopwatch();

			timerElapsed += state => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedViolations)));

			if (Settings.UseAlerts)
				timerElapsed += InvokeAlert;

			timer = new Timer(timerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		public void StopTimer()
		{
			timer.Change(0, Timeout.Infinite);
			stopwatch.Stop();
			IsTimerStarted = false;
		}

		public void StartTimer()
		{
			timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
			stopwatch.Start();
			IsTimerStarted = true;

			if (IsDoubleHitsInRow && currentPhrase > previousPhrase)
				IsDoubleHitsInRow = false;

			previousPhrase = currentPhrase;
		}

		public void Reset()
		{
			stopwatch.Reset();
			IsTimerStarted = false;
			BlueScore = 0;
			RedScore = 0;
			DoubleHits = 0;
			RedViolations = 0;
			BlueViolations = 0;
			IsDoubleHitsInRow = true;
		}

		private struct Phrase
		{
			public int BlueScore;
			public int RedScore;

			public static bool operator >(Phrase phase1, Phrase phase2)
			{
				return phase1.BlueScore > phase2.BlueScore ||
						phase1.RedScore > phase2.RedScore;
			}

			public static bool operator <(Phrase phase1, Phrase phase2)
			{
				throw new NotImplementedException();
			}
		}

		private void InvokeAlert(object state)
		{
			if (Settings.Alerts.Any(a => a.TotalSeconds == Elapsed.TotalSeconds))
				TimeAlert?.Invoke();
		}
	}
}
