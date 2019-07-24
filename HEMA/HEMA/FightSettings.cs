using System;
using System.Collections.Generic;
using System.Text;

namespace HEMA
{
	public class FightSettings
	{
		public int DoubleHitsCommon { get; set; }

		public int DoubleHitsInARow { get; set; }

		public int ViolationsCount { get; set; }

		public int PenaltyPoints { get; set; }

		public int MaxFightScore { get; set; }

		public bool UseFightSettings { get; set; }

		public bool UseAlerts { get; set; }

		public List<TimeSpan> Alerts { get; set; }

		public FightSettings()
		{
			SetDefaults();
		}

		public void SetDefaults()
		{
			DoubleHitsCommon = 4;
			DoubleHitsInARow = 3;
			PenaltyPoints = 1;
			ViolationsCount = 2;
			MaxFightScore = 10;
			UseAlerts = true;
			UseFightSettings = true;
			Alerts = new List<TimeSpan> { new TimeSpan(0, 2, 0) };
		}
	}
}
