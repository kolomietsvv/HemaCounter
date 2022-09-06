using Android.Media;
using HEMA.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace HEMA
{
    public partial class MainPage : ContentPage
    {
        private string settingsPath;
        private CommonSettingsPage commonSettingsPage;
        private TimerSettingsPage timerSettingsPage;
        private Color btnsColor;
        private bool alarmIsOn;
        private bool pauseFight;
        private bool checkAlarms;
        private MediaPlayer tickMediaPlayer;
        private MediaPlayer alarmMediaPlayer;
        private MediaPlayer currentMediaPlayer;
        private List<TimerAlarmLight> alarmsInUse;

        private UserDeclines userDeclines;

        public Fight Fight { get; }

        public Color BtnsColor
        {
            get => btnsColor;
            set
            {
                btnsColor = value;
                OnPropertyChanged(nameof(BtnsColor));
            }
        }

        public bool IsSettingsEnabled => !Fight.IsFightStarted;

        public Color SettingsBtnColor => Fight.IsFightStarted ? Color.LightSlateGray : Color.White;

        public ICommand ResetSettingsCmd => new Command(Fight.Settings.SetDefaults);

        public MainPage(MediaPlayer tickMediaPlayer, MediaPlayer alarmMediaPlayer)
        {
            InitializeComponent();
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.json");
            FightSettings settings = GetFightSettings();
            this.tickMediaPlayer = tickMediaPlayer;
            this.alarmMediaPlayer = alarmMediaPlayer;
            currentMediaPlayer = tickMediaPlayer;
            userDeclines = new UserDeclines();
            Fight = new Fight(settings);
            Fight.OneDoubleHitLeft += isOneDoubleHitLeft => DoubleHitlLbl.TextColor = isOneDoubleHitLeft ? Color.Red : Color.Default;
            Fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog(TextCollection.MaxDoubleHits, FinishCause.DoubleHits);
            BindingContext = this;
            commonSettingsPage = new CommonSettingsPage();
            commonSettingsPage.BindingContext = this;
            timerSettingsPage = new TimerSettingsPage();
            foreach (var alarm in Fight.Alarms)
            {
                timerSettingsPage.AddItem(null, null);
            }
            timerSettingsPage.BindingContext = this;
            timerSettingsPage.ItemAdded += AddAlarmSettings;
            timerSettingsPage.ItemRemoved += RemoveAlarmSettings;
            Fight.TimerTick += PlaySound;
            if (Fight.Settings.NoBreak)
            {
                BtnsColor = Color.WhiteSmoke;
            }
            else
            {
                BtnsColor = Color.LightSlateGray;
            }
            UpdateSettingsEnabled();
        }

        private void RemoveAlarmSettings(int index)
        {
            Fight.Alarms.RemoveAt(index);
        }

        private void AddAlarmSettings(int index)
        {
            if (Fight.Alarms.Count < index + 1)
            {
                Fight.Alarms.Add(new Models.TimerAlarm
                {
                    IsOn = true,
                });
            }
        }

        private void OpenSettingsTab(object sender, EventArgs e)
        {
            Navigation.PushAsync(commonSettingsPage);
        }

        private void OpenAlarmTab(object sender, EventArgs e)
        {
            Navigation.PushAsync(timerSettingsPage);
        }

        private void StartTimer(object sender, EventArgs e)
        {
            if (Fight.IsTimerStarted)
            {
                Fight.PauseTimer();
                SetColorsOnPause();
            }
            else
            {
                alarmsInUse = Fight.Alarms
                    .Where(alarm => alarm.IsOn && alarm.TotalSeconds > Fight.Elapsed.TotalSeconds)
                    .Select(alarm => alarm.AlarmLight)
                    .OrderByDescending(alarmLight => alarmLight.TotalSeconds)
                    .ToList();
                if (alarmsInUse.Count > 0 && !checkAlarms)
                {
                    Fight.TimerTick += CheckAlarm;
                    checkAlarms = true;
                }
                Fight.StartTimer();
                SetColorsOnStart();
            }
            UpdateSettingsEnabled();
        }

        private void UpdateSettingsEnabled()
        {
            OnPropertyChanged(nameof(IsSettingsEnabled));
            OnPropertyChanged(nameof(SettingsBtnColor));
        }

        private void ResetTimer(object sender, EventArgs e)
        {
            DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
            userDeclines.Reset();
            UpdateSettingsEnabled();
        }

        private void DecreaseBlueScore(object sender, EventArgs e)
        {
            if (Fight.IsScoreChangeEnabled && Fight.BlueScore > 0)
            {
                Fight.BlueScore--;
            }
        }

        private void DecreaseRedScore(object sender, EventArgs e)
        {
            if (Fight.IsScoreChangeEnabled && Fight.RedScore > 0)
            {
                Fight.RedScore--;
            }
        }

        private void IncreaseBlueScore(object sender, EventArgs e)
        {
            if (Fight.IsScoreChangeEnabled)
            {
                Fight.BlueScore++;
            }
        }

        private void IncreaseRedScore(object sender, EventArgs e)
        {
            if (Fight.IsScoreChangeEnabled)
            {
                Fight.RedScore++;
            }
        }

        private void DecreaseDoubleHits(object sender, EventArgs e)
        {
            if (Fight.IsScoreChangeEnabled && Fight.DoubleHits > 0)
            {
                Fight.DoubleHits--;
            }
        }

        private void IncreaseDoubleHits(object sender, EventArgs e)
        {
            Fight.DoubleHits++;
        }

        private void DecreaseRedViolations(object sender, EventArgs e)
        {
            Fight.RedViolations--;
        }

        private void IncreaseRedViolations(object sender, EventArgs e)
        {
            Fight.RedViolations++;
        }

        private void DecreaseBlueViolations(object sender, EventArgs e)
        {
            Fight.BlueViolations--;
        }

        private void IncreaseBlueViolations(object sender, EventArgs e)
        {
            Fight.BlueViolations++;
        }

        private async void DisplayFinishFightDialog(string cause, FinishCause finishCause)
        {
            switch (finishCause)
            {
                case FinishCause.DoubleHits:
                    if (userDeclines.UserDeclinedDoubleHitsFinish)
                        return;
                    break;
                case FinishCause.Time:
                    if (userDeclines.UserDeclinedTimeFinish)
                        return;
                    break;
            }

            Fight.PauseTimer();
            var userChoice = await DisplayAlert(TextCollection.FightIsOver, cause, TextCollection.Finish, TextCollection.Continue);
            if (userChoice)
                Fight.Reset();

            else
                switch (finishCause)
                {
                    case FinishCause.DoubleHits:
                        userDeclines.UserDeclinedDoubleHitsFinish = true;
                        break;
                    case FinishCause.Time:
                        userDeclines.UserDeclinedTimeFinish = true;
                        break;
                }
        }

        private FightSettings GetFightSettings()
        {
            FightSettings settings;
            if (File.Exists(settingsPath))
            {
                var settingsString = File.ReadAllText(settingsPath);
                settings = JsonConvert.DeserializeObject<FightSettings>(settingsString);
            }
            else
            {
                settings = new FightSettings();
            }

            return settings;
        }

        protected override void OnAppearing()
        {
            var settingsString = JsonConvert.SerializeObject(Fight.Settings);
            File.WriteAllText(settingsPath, settingsString);
            base.OnAppearing();
        }

        private void SetColorsOnPause()
        {
            StartBtn.TextColor = Color.LightSlateGray;
            if (!Fight.Settings.NoBreak)
            {
                BtnsColor = Color.WhiteSmoke;
            }
        }

        private void SetColorsOnStart()
        {
            StartBtn.TextColor = Color.Default;
            if (!Fight.Settings.NoBreak)
            {
                BtnsColor = Color.LightSlateGray;
            }
            else
            {
                BtnsColor = Color.WhiteSmoke;
            }
        }

        private void PlaySound()
        {
            currentMediaPlayer.Start();
        }

        private void CheckAlarm()
        {
            var lastIndex = alarmsInUse.Count - 1;
            if (lastIndex < 0)
            {
                if (alarmIsOn)
                {
                    currentMediaPlayer = tickMediaPlayer;
                    alarmIsOn = false;
                }
                if (pauseFight)
                {
                    Fight.PauseTimer();
                }
                checkAlarms = false;
                Fight.TimerTick -= CheckAlarm;
                return;
            }

            var alarmShouldBeTurnedOn = false;
            pauseFight = alarmsInUse[lastIndex].PauseFight;
            var offset = Fight.Elapsed.TotalSeconds - alarmsInUse[lastIndex].TotalSeconds;
            alarmShouldBeTurnedOn = offset >= -1d && offset < 2d;

            if (!alarmIsOn && alarmShouldBeTurnedOn)
            {
                currentMediaPlayer = alarmMediaPlayer;
                alarmIsOn = true;
                alarmsInUse.RemoveAt(lastIndex);
                return;
            }

            if (alarmIsOn && !alarmShouldBeTurnedOn)
            {
                currentMediaPlayer = tickMediaPlayer;
                alarmIsOn = false;
            }
        }
    }

    struct UserDeclines
    {
        public bool UserDeclinedDoubleHitsFinish;
        public bool UserDeclinedTimeFinish;

        public void Reset()
        {
            UserDeclinedDoubleHitsFinish = false;
            UserDeclinedTimeFinish = false;
        }
    }

    enum FinishCause
    {
        DoubleHits = 1,
        MaxScore = 2,
        Time = 3,
        Manual = 4,
    }
}
