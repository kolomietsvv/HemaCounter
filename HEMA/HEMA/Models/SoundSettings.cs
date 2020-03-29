using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HEMA
{
	public class SoundSettings : INotifyPropertyChanged
	{
		private bool isSoundEnabled = true;
		private bool isVibrationEnabled = true;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsSoundEnabled
		{
			get => isSoundEnabled;
			set
			{
				isSoundEnabled = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSoundEnabled)));
			}
		public bool IsVibrationEnabled
		{
			get => isVibrationEnabled;
			set
			{
				isVibrationEnabled = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVibrationEnabled)));
			}
		}

		public List<TimeSpan> TimeNotifications { get; set; } = new List<TimeSpan>();
	}
}
