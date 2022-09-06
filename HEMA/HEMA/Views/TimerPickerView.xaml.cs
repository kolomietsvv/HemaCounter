using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Xamarin.Forms.BindableProperty;

namespace HEMA.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TimerPickerView : ContentView
    {
        public int Index { get; set; }

        public int Minutes
        {
            get { return (int)GetValue(MinutesProperty); }
            set { SetValue(MinutesProperty, value); }
        }
        public static readonly BindableProperty MinutesProperty;
        public static void MinutesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((TimerPickerView)bindable).MinutesEntry.Text = ((int)newValue).ToString("D2");
        }

        public int Seconds
        {
            get { return (int)GetValue(SecondsProperty); }
            set { SetValue(SecondsProperty, value); }
        }
        public static BindableProperty SecondsProperty;
        public static void SecondsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((TimerPickerView)bindable).SecondsEntry.Text = ((int)newValue).ToString("D2");
        }

        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }
        public static BindableProperty IsOnProperty;
        public static void IsOnChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((TimerPickerView)bindable).IsOnToggle.IsToggled = (bool)newValue;
        }

        public bool PauseFight
        {
            get { return (bool)GetValue(PauseFightProperty); }
            set { SetValue(PauseFightProperty, value); }
        }
        public static BindableProperty PauseFightProperty;
        public static void PauseFightChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((TimerPickerView)bindable).PauseFightToggle.IsToggled = (bool)newValue;
        }

        static TimerPickerView()
        {
            SecondsProperty = BindableProperty.Create(nameof(SecondsProperty), typeof(int),
            typeof(TimerPickerView), defaultValue: -1, propertyChanged: new BindingPropertyChangedDelegate(SecondsChanged));
            MinutesProperty = BindableProperty.Create(nameof(MinutesProperty), typeof(int),
            typeof(TimerPickerView), defaultValue: -1, propertyChanged: new BindingPropertyChangedDelegate(MinutesChanged));
            IsOnProperty = BindableProperty.Create(nameof(IsOnProperty), typeof(bool),
            typeof(TimerPickerView), propertyChanged: new BindingPropertyChangedDelegate(IsOnChanged));
            PauseFightProperty = BindableProperty.Create(nameof(PauseFightProperty), typeof(bool),
            typeof(TimerPickerView), propertyChanged: new BindingPropertyChangedDelegate(PauseFightChanged));
        }

        public TimerPickerView()
        {
            InitializeComponent();
        }

        public void RemoveItem(object sender, EventArgs e)
        {
            var children = ((StackLayout)Parent).Children;
            for (int i = Index + 1; i < children.Count; i++)
            {
                ((TimerPickerView)children[i]).Index--;
            }
            children.RemoveAt(Index);
        }

        private void RemoveExtraCharacters(object sender, TextChangedEventArgs e)
            => TextHelper.RemoveExtraCharacters(sender, e);
    }
}