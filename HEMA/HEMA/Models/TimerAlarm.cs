using Android.OS;
using Java.IO;
using System.ComponentModel;

namespace HEMA.Models
{
    public class TimerAlarm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int minutes { get; set; }
        private int seconds { get; set; }
        private bool isOn { get; set; }
        private bool pauseFight { get; set; }

        public int Minutes
        {
            get => minutes;
            set
            {
                minutes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Minutes)));
            }
        }

        public int Seconds
        {
            get => seconds;
            set
            {
                seconds = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seconds)));
            }
        }

        public bool IsOn
        {
            get => isOn;
            set
            {
                isOn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOn)));
            }
        }

        public bool PauseFight
        {
            get => pauseFight;
            set
            {
                pauseFight = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PauseFight)));
            }
        }

        public int TotalSeconds => minutes * 60 + seconds;

        public TimerAlarmLight AlarmLight 
        {
            get => new TimerAlarmLight
                { 
                    TotalSeconds = TotalSeconds,
                    PauseFight = PauseFight
                }; 
        }
    }

    public struct TimerAlarmLight
    {
        public int TotalSeconds;
        public bool PauseFight;
    }
}
