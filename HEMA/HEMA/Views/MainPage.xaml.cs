using Android.Media;
using HEMA.Models;
using HEMA.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace HEMA
{
    public partial class MainPage : ContentPage
    {
        private string settingsPath;
        private string alarmsPath;
        private CommonSettingsPage commonSettingsPage;
        private TimerSettingsPage timerSettingsPage;
        private ProtocolItemPage protocolItemPage;
        private Color btnsColor;
        private bool alarmIsOn;
        private bool pauseFight;
        private bool checkAlarm;
        private MediaPlayer tickMediaPlayer;
        private MediaPlayer alarmMediaPlayer;
        private MediaPlayer alarmPauseMediaPlayer;
        private MediaPlayer currentMediaPlayer;
        private List<TimerAlarmLight> alarmsInUse;

        private List<Protocol> protocols;
        private Protocol currentProtocol;

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

        public MainPage(MediaPlayer tickMediaPlayer, MediaPlayer alarmMediaPlayer, MediaPlayer alarmPauseMediaPlayer)
        {
            InitializeComponent();
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.json");
            alarmsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "alarms.json");
            FightSettings settings = GetFightSettings();
            List<TimerAlarm> alarms = GetAlarmSettings();
            this.tickMediaPlayer = tickMediaPlayer;
            this.alarmMediaPlayer = alarmMediaPlayer;
            this.alarmPauseMediaPlayer = alarmPauseMediaPlayer;
            currentMediaPlayer = tickMediaPlayer;
            userDeclines = new UserDeclines();
            Fight = new Fight("", "", settings, alarms);
            Fight.OneDoubleHitLeft += isOneDoubleHitLeft => DoubleHitlLbl.TextColor = isOneDoubleHitLeft ? Color.Red : Color.Default;
            Fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog(TextCollection.MaxDoubleHits, FinishCause.DoubleHits);
            BindingContext = this;
            commonSettingsPage = new CommonSettingsPage();
            commonSettingsPage.BindingContext = this;
            timerSettingsPage = new TimerSettingsPage();
            protocolItemPage = new ProtocolItemPage();
            commonSettingsPage.BindingContext = this;
            protocols = new List<Protocol>();
            foreach (var alarm in Fight.Alarms)
            {
                timerSettingsPage.AddItem(null, null);
            }
            timerSettingsPage.BindingContext = this;
            timerSettingsPage.ItemAdded += AddAlarmSettings;
            TimerPickerView.ItemRemoved += RemoveAlarmSettings;
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
            this.alarmPauseMediaPlayer = alarmPauseMediaPlayer;
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

        private void ShowProtocolEditView(object sender, EventArgs e)
        {
            Navigation.PushAsync(protocolItemPage);
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
                if (!checkAlarm && currentMediaPlayer != tickMediaPlayer)
                {
                    currentMediaPlayer = tickMediaPlayer;
                }
                Fight.PauseTimer();
                SetColorsOnPause();
                currentProtocol.Exchanges.Add(new ProtocolItem
                    {
                        RedScore = Fight.RedScore,
                        RedViolations = Fight.RedViolations,
                        BlueScore = Fight.BlueScore,
                        BlueViolations = Fight.BlueViolations,
                        Time = Fight.Elapsed,
                        DoubleHits = Fight.DoubleHits
                    });
            }
            else
            {
                FillAlarms();
                SetColorsOnStart();
                Fight.StartTimer();
                currentProtocol = new Protocol();
                protocols.Add(currentProtocol);
            }
            UpdateSettingsEnabled();
        }

        private void FillAlarms()
        {
            alarmsInUse = Fight.Alarms
                .Where(alarm => alarm.IsOn && alarm.TotalSeconds > Fight.Elapsed.TotalSeconds)
                .Select(alarm => alarm.AlarmLight)
                .OrderByDescending(alarmLight => alarmLight.TotalSeconds)
                .ToList();
            checkAlarm = alarmsInUse.Count > 0;
        }

        private void UpdateSettingsEnabled()
        {
            OnPropertyChanged(nameof(IsSettingsEnabled));
            OnPropertyChanged(nameof(SettingsBtnColor));
        }

        private void ResetTimer(object sender, EventArgs e)
        {
            DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
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
            {
                Fight.Reset();
                userDeclines.Reset();
                UpdateSettingsEnabled();
                checkAlarm = false;
            }

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

        private List<TimerAlarm> GetAlarmSettings()
        {
            List<TimerAlarm> alarms;
            if (File.Exists(alarmsPath))
            {
                var alarmsString = File.ReadAllText(alarmsPath);
                alarms = JsonConvert.DeserializeObject<List<TimerAlarm>>(alarmsString);
            }
            else
            {
                alarms = new List<TimerAlarm>();
            }

            return alarms;
        }

        protected override void OnAppearing()
        {
            var settingsString = JsonConvert.SerializeObject(Fight.Settings);
            File.WriteAllText(settingsPath, settingsString);
            var alarmsString = JsonConvert.SerializeObject(Fight.Alarms.ToList());
            File.WriteAllText(alarmsPath, alarmsString);
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
            var lastIndex = alarmsInUse.Count - 1;

            if (checkAlarm)
            {
                if (lastIndex >= 0)
                {
                    var offset = Fight.Elapsed.TotalSeconds - alarmsInUse[lastIndex].TotalSeconds;
                    bool alarmShouldBeTurnedOn = offset >= 0 && offset < 1d;

                    if (alarmShouldBeTurnedOn)
                    {
                        pauseFight = alarmsInUse[lastIndex].PauseFight;
                        alarmsInUse.RemoveAt(lastIndex);
                        if (!alarmIsOn)
                        {
                            if (!pauseFight)
                            {
                                currentMediaPlayer = alarmMediaPlayer;
                            }
                            else
                            {
                                currentMediaPlayer = alarmPauseMediaPlayer;
                            }
                            alarmIsOn = true;
                        }
                        if (pauseFight)
                        {
                            Fight.PauseTimer();
                        }
                    }
                    else if (alarmIsOn)
                    {
                        alarmIsOn = false;
                        currentMediaPlayer = tickMediaPlayer;
                    }
                }
                else
                {
                    checkAlarm = false;
                }
            }
            if (alarmIsOn)
            {
                alarmIsOn = false;
                currentMediaPlayer = tickMediaPlayer;
            }

            currentMediaPlayer.Start();
        }
    }
}
