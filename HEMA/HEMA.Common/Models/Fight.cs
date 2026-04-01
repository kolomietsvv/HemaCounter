using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Input;

using HEMA.Common.Models;
using HEMA.Models;

namespace HEMA
{
	public class Fight : INotifyPropertyChanged
	{
		private OffsetStopwatch stopwatch;
		private Timer timer;
		private Phrase previousPhrase;
		private Phrase currentPhrase;

		private bool isTimerStarted;
		private bool isOneDoubleHitLeft;
		private bool isDoubleHitsInRow;

		private int doubleHits;
		private int blueViolations;
		private int redViolations;

		private string redName;
		private string blueName;
		private string redRaitingChange;
		private string blueRaitingChange;
		private string? subgroupName;
		private string? nominationName;
		private int originalIndex;
		private bool isCompleted;
		private int nextAlarmIndex;

		public event Action MaxDoubleHitsReached;
		public event Action TimerTick;
		public event Action<bool> OneDoubleHitLeft;
		public event PropertyChangedEventHandler PropertyChanged;

		[JsonIgnore]
		public ICommand EditFightCommand { get; set; }

		public NextFightInfo? WinnerNextFightInfo { get; set; }
		
		public NextFightInfo? LooserNextFightInfo { get; set; }

		public string? Title { get; set; }

		public BracketInfo BracketInfo { get; set; }

		public int? MaxDoubleHits { get; set; }

		public FightSettings Settings { get; set; }

		public TimerAlarmLight[] TimerAlarms { get; set; }

		public int NextAlarmIndex
		{
			get => nextAlarmIndex;
			set => nextAlarmIndex = TimerAlarms?.Length > value ? value : -1;
		}

		public TimerAlarmLight? NextAlarm => NextAlarmIndex != -1 && TimerAlarms != null ? TimerAlarms[NextAlarmIndex] : (TimerAlarmLight?)null;

		public bool IsScoreChangeEnabled => !IsTimerStarted || Settings.NoBreak;

		public string? NominationName
		{
			get => nominationName;
			set
			{
				nominationName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NominationName)));
			}
		}

		public string? SubgroupName
		{
			get => subgroupName;
			set
			{
				subgroupName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubgroupName)));
			}
		}

		public int OriginalIndex
		{
			get => originalIndex;
			set
			{
				originalIndex = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
			}
		}

		public bool IsCompleted
		{
			get => isCompleted;
			set
			{
				isCompleted = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
			}
		}

		public void SetName(FighterColor fighterColor, string name)
		{
			switch (fighterColor)	
			{
				case FighterColor.Red:
					RedName = name;
					return;
				case FighterColor.Blue:
					BlueName = name;
					return;
			}
		}

		public string RedName
		{
			get => redName;
			set
			{
				redName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedName)));
			}
		}

		public string BlueName
		{
			get => blueName;
			set
			{
				blueName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueName)));
			}
		}

		public string RedRaitingChange
		{
			get => redRaitingChange;
			set
			{
				redRaitingChange = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedRaitingChange)));
			}
		}

		public string BlueRaitingChange
		{
			get => blueRaitingChange;
			set
			{
				blueRaitingChange = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueRaitingChange)));
			}
		}

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
				BlueScore = CalculateScore(value, blueViolations, BlueScore);
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
				RedScore = CalculateScore(value, redViolations, RedScore);
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
				if (Settings.NoBreak)
					UpdateDoubleHitsInRowFlagAndFrase();
				//annoing pop-up
				//if (Settings.UseFightSettings && value >= Settings.MaxFightScore)
				//	MaxScoreReached?.Invoke();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedScore)));
			}
		}

		public int BlueScore
		{
			get => currentPhrase.BlueScore;
			set
			{
				currentPhrase.BlueScore = value;
				if (Settings.NoBreak)
					UpdateDoubleHitsInRowFlagAndFrase();
				//annoing pop-up
				//if (Settings.UseFightSettings && value >= Settings.MaxFightScore)
				//	MaxScoreReached?.Invoke();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlueScore)));
			}
		}

		public TimeSpan Elapsed
		{
			get => (stopwatch?.Elapsed).GetValueOrDefault();
			set
			{
				stopwatch = new OffsetStopwatch(value);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Elapsed)));
			}
		}

		public bool IsFightStarted => stopwatch != null && stopwatch.Elapsed != TimeSpan.Zero;

		public Fight(string redName, string blueName, FightSettings settings)
			: this()
		{
			Settings = settings;
			MaxDoubleHits = isDoubleHitsInRow ? Settings?.DoubleHitsInARow : Settings?.DoubleHitsCommon;

			RedName = redName;
			BlueName = blueName;
		}

		[Obsolete("Только для сериализатора")]
		public Fight()
		{
			stopwatch = new OffsetStopwatch();

			var timerCallback = new TimerCallback(UpdateElapsedProperty);
			timerCallback += InvokeTimerTick;
			timer = new Timer(timerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			timer.Change(0, Timeout.Infinite);

			OneDoubleHitLeft += value => isOneDoubleHitLeft = value;
		}

		public void PauseTimer()
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

			UpdateDoubleHitsInRowFlagAndFrase();
			UpdateElapsedProperty();
		}

		public void Reset()
		{
			stopwatch.Reset();
			PauseTimer();
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

		private void InvokeTimerTick(object state)
		{
			if (stopwatch.IsRunning && stopwatch.Elapsed.TotalSeconds >= 1)
				TimerTick?.Invoke();
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

		private int CalculateScore(int value, int previousValue, int score)
		{
			var isIncreased = previousValue < value;
			if (isIncreased && Settings.UseFightSettings && value >= Settings.ViolationsToStartPenalize)
				score -= Settings.PenaltyPoints;
			else if (!isIncreased && Settings.UseFightSettings && previousValue >= Settings.ViolationsToStartPenalize)
				score += Settings.PenaltyPoints;
			return score;
		}

		private void UpdateElapsedProperty(object state = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Elapsed)));
		}

		private void NotificateAboutOneDoubleHitLeft()
		{
			if (doubleHits + 1 == MaxDoubleHits)
				OneDoubleHitLeft?.Invoke(true);
			else if (isOneDoubleHitLeft && doubleHits + 1 < MaxDoubleHits)
				OneDoubleHitLeft?.Invoke(false);
		}

		private void UpdateDoubleHitsInRowFlagAndFrase()
		{
			if (DoubleHits != 0 && IsDoubleHitsInRow && currentPhrase > previousPhrase)
				IsDoubleHitsInRow = false;
			previousPhrase = currentPhrase;
		}
		#endregion
	}
}
