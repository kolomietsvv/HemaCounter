using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HEMA
{
	class SoundSettings : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsSoundEnabled { get; set; }

		public bool IsVibrationEnabled { get; set; }

		public List<TimeSpan> TimeNotifications { get; set; }
	}
}
