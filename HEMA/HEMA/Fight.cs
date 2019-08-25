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
		private bool isOneDoubleHitLeft;
		private bool isDoubleHitsInRow;

		private int doubleHits;
		private int blueViolations;
		private int redViolations;

		public event Action MaxDoubleHitsReached;
		public event Action MaxScoreReached;
		public event Action TimeAlert;
		public event Action<bool> IsOneDoubleHitLeft;
		public event PropertyChangedEventHandler PropertyChanged;

		private int MaxDoubleHits => isDoubleHitsInRow ? Settings.DoubleHitsInARow : Settings.DoubleHitsCommon;

		public FightSettings Settings { get; }

		public bool IsScoreChangeEnabled => !IsTimerStarted && IsFightStarted || IsFightStarted && Settings.NoBreak;

		public bool IsTimerStarted
		{
			get => isTimerStarted;
			private set
			{
				isTimerStarted = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTimerStarted)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangeEnabled)));
			}
		}

		public bool IsDoubleHitsInRow
		{
			get => isDoubleHitsInRow;
			private set
			{
				isDoubleHitsInRow = value;
				if (Settings.UseFightSettings)
					NotificateAboutOneDoubleHitLeft();
			}
		}

		public int DoubleHits
		{
			get => doubleHits;
			set
			{
				doubleHits = value;
				if (Settings.UseFightSettings)
				{
					if (doubleHits == MaxDoubleHits)
						MaxDoubleHitsReached?.Invoke();
					else
						NotificateAboutOneDoubleHitLeft();
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoubleHits)));
			}
		}

		public int BlueViolations
		{
			get => blueViolations;
			set
			{
				if (value < 0)
					return;
				BlueScore = CaclulateScore(value, blueViolations, BlueScore);
				blueViolations = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueViolations)));
			}
		}

		public int RedViolations
		{
			get => redViolations;
			set
			{
				if (value < 0)
					return;
				RedScore = CaclulateScore(value, redViolations, RedScore);
				redViolations = value;
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

			if (Settings.UseAlerts)
				timerElapsed += InvokeAlert;

			timer = new Timer(UpdateElapsedProperty, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

			IsOneDoubleHitLeft += value => isOneDoubleHitLeft = value;
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
			UpdateElapsedProperty();
		}

		public void Reset()
		{
			stopwatch.Reset();
			IsTimerStarted = false;
			currentPhrase = new Phrase();
			DoubleHits = 0;
			BlueViolations = 0;
			RedViolations = 0;
			BlueScore = 0;
			RedScore = 0;
			IsDoubleHitsInRow = true;
			UpdateElapsedProperty();
		}

		#region private

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

		private int CaclulateScore(int value, int previousValue, int score)
		{
			var isIncreased = previousValue < value;
			if (isIncreased && Settings.UseFightSettings && value >= Settings.ViolationsToStartPenalize)
				score -= Settings.PenaltyPoints;
			else if (!isIncreased && Settings.UseFightSettings && previousValue >= Settings.ViolationsToStartPenalize)
				score += Settings.PenaltyPoints;
			return score;
		}

		private void InvokeAlert(object state)
		{
			if (Settings.Alerts.Any(a => a.TotalSeconds == Elapsed.TotalSeconds))
				TimeAlert?.Invoke();
		}

		private void UpdateElapsedProperty(object state = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Elapsed)));
		}

		private void NotificateAboutOneDoubleHitLeft()
		{
			if (doubleHits + 1 == MaxDoubleHits)
				IsOneDoubleHitLeft?.Invoke(true);
			else if (isOneDoubleHitLeft && doubleHits + 1 < MaxDoubleHits)
				IsOneDoubleHitLeft?.Invoke(false);
		}

		#endregion
	}
}
