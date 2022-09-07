using HEMA.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HEMA
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TimerSettingsPage : ContentPage
    {
        public event Action<int> ItemAdded;
        private bool isManual = true;

        public TimerSettingsPage()
        {
            InitializeComponent();
            BindingContext = App.Current.MainPage;
            //AlarmsList.ChildRemoved += (s, e) => ItemRemoved?.Invoke(((TimerPickerView)e.Element).Index);
            //TimerPickerView.AnyPauseFightTurnedOff += () =>
            //{
            //    isManual = false;
            //    AllPauseFightToggle.IsToggled = false;
            //};
            //TimerPickerView.AnyIsOnTurnedOff += () => AllIsOnToggle.IsToggled = false;
        }

        public void AddItem(object sender, EventArgs args)
        {
            var newIndex = AlarmsList.Children.Count;
            ItemAdded?.Invoke(newIndex);
            var minutesBinding = new Binding($"Fight.Alarms[{newIndex}].Minutes");
            var secondsBinding = new Binding($"Fight.Alarms[{newIndex}].Seconds");
            var isOnBinding = new Binding($"Fight.Alarms[{newIndex}].IsOn");
            var pauseFightBinding = new Binding($"Fight.Alarms[{newIndex}].PauseFight");
            var item = new TimerPickerView();
            item.Index = newIndex;
            item.SetBinding(TimerPickerView.MinutesProperty, minutesBinding);
            item.SetBinding(TimerPickerView.SecondsProperty, secondsBinding);
            item.SetBinding(TimerPickerView.IsOnProperty, isOnBinding);
            item.SetBinding(TimerPickerView.PauseFightProperty, pauseFightBinding);
            AlarmsList.Children.Add(item);
        }

        private void SetAllIsOn(object sender, ToggledEventArgs args)
        {
            if (isManual)
            {
                foreach (var child in AlarmsList.Children)
                {
                    ((TimerPickerView)child).IsOn = args.Value;
                }
            }
            isManual = true;
        }

        private void SetAllPauseFight(object sender, ToggledEventArgs args)
        {
            if (isManual)
            {
                foreach (var child in AlarmsList.Children)
                {
                    ((TimerPickerView)child).PauseFight = args.Value;
                }
            }
            isManual = true;
        }
    }
}