using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HEMA
{
	public class FightSettings : INotifyPropertyChanged
	{
		public int doubleHitsCommon;
		public int doubleHitsInARow;
		public int violationsToStartPenalize;
		public int penaltyPoints;
		public int maxFightScore;
		public bool useFightSettings;
		public bool useAlerts;
		private bool noBreak;

		public event PropertyChangedEventHandler PropertyChanged;

		public int DoubleHitsCommon
		{
			get => doubleHitsCommon; set
			{
				doubleHitsCommon = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoubleHitsCommon)));
			}
		}

		public int DoubleHitsInARow
		{
			get => doubleHitsInARow;
			set
			{
				doubleHitsInARow = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoubleHitsInARow)));
			}
		}

		public int ViolationsToStartPenalize
		{
			get => violationsToStartPenalize;
			set
			{
				violationsToStartPenalize = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViolationsToStartPenalize)));
			}
		}

		public int PenaltyPoints
		{
			get => penaltyPoints; set
			{
				penaltyPoints = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PenaltyPoints)));
			}
		}

		public int MaxFightScore
		{
			get => maxFightScore;
			set
			{
				maxFightScore = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxFightScore)));
			}
		}

		public bool UseFightSettings
		{
			get => useFightSettings;
			set
			{
				useFightSettings = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseFightSettings)));
			}
		}

		public bool UseAlerts
		{
			get => useAlerts; set
			{
				useAlerts = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseAlerts)));
			}
		}


		public bool NoBreak
		{
			get => noBreak;
			set
			{
				noBreak = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NoBreak)));
			}
		}

		public List<TimeSpan> Alerts { get; private set; }

		public FightSettings()
		{
			SetDefaults();
		}

		public void SetDefaults()
		{
			DoubleHitsCommon = 4;
			DoubleHitsInARow = 3;
			PenaltyPoints = 1;
			ViolationsToStartPenalize = 2;
			MaxFightScore = 10;
			UseAlerts = true;
			UseFightSettings = true;
			NoBreak = false;
			Alerts = new List<TimeSpan> { new TimeSpan(0, 2, 0) };
		}
	}
}
