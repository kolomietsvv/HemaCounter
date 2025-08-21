using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

using HEMA.Models;

using Microsoft.Win32;

using Color = System.Drawing.Color;


namespace HEMA.WpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private Fight fight;
		private Fight nextFight;
		private Color btnsColor;
		private UserDeclines userDeclines;
		private FightsListWindow fightsListWindow;
		private IEnumerator<Fight> enumerator;
		private string currentFolder;
		private MediaPlayer[] mediaPlayers;
		private TimerAlarmLight[] ttmerAlarms =
			[
				new TimerAlarmLight { TotalSeconds = 105},
				new TimerAlarmLight { TotalSeconds = 120, PauseFight = true }
			];

		public event PropertyChangedEventHandler? PropertyChanged;
		public ObservableCollection<Fight> Fights { get; }

		public Fight Fight
		{
			get => fight;
			set
			{
				fight = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fight)));
			}
		}

		public Fight NextFight
		{
			get => nextFight;
			set
			{
				nextFight = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NextFight)));
			}
		}

		public Color BtnsColor
		{
			get => btnsColor;
			set
			{
				btnsColor = value;
			}
		}

		public bool IsSettingsEnabled => !Fight.IsFightStarted;

		public MainWindow()
		{
			InitializeComponent();
			mediaPlayers = [new MediaPlayer(), new MediaPlayer()];

			Fights = new ObservableCollection<Fight>();
			InitFights(["Боец 1", "Боец 2", "Боец 3"]);

			DataContext = this;
			fightsListWindow = new FightsListWindow(this);
		}

		private async void ShowPopupTime_Click(object sender, RoutedEventArgs e)
		{
			Fight.PauseTimer();

			var popup = new PopupTimeSpanWindow(Fight.Elapsed)
			{
				Owner = this // чтобы по Alt+Tab не терялся
			};

			// Небольшая анимация появления (опционально)
			popup.Opacity = 0;
			await Task.Delay(10);
			var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 0.98, new Duration(TimeSpan.FromMilliseconds(160)));
			popup.BeginAnimation(OpacityProperty, anim);

			if (popup.ShowDialog().GetValueOrDefault())
			{
				Fight.Elapsed = popup.Value;
				if (popup.Value.TotalSeconds <= ttmerAlarms[0].TotalSeconds)
				{
					Fight.NextAlarmIndex = 0;
					mediaPlayers[0].Stop();
					mediaPlayers[0].Open(new Uri(@"C:\Vika\HemaCounter\HEMA\HEMA.Android\Resources\raw\beep.mp3", UriKind.Absolute));
				}
				else if (popup.Value.TotalSeconds <= ttmerAlarms[0].TotalSeconds)
				{
					Fight.NextAlarmIndex = 1;
					mediaPlayers[1].Stop();
					mediaPlayers[1].Open(new Uri(@"C:\Vika\HemaCounter\HEMA\HEMA.Android\Resources\raw\longBeep.mp3", UriKind.Absolute));
				}

			}
		}

		private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
		{
			var fileDialog = new OpenFileDialog
			{
				Title = "Выберите файл",
				Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
			};

			var folderDialog = new OpenFolderDialog
			{
				Title = "Выберите папку логирования",
			};

			if (fileDialog.ShowDialog().GetValueOrDefault())
			{
				await TrySaveStateAsync();

				var extension = Path.GetExtension(fileDialog.FileName);
				if (extension == ".json")
				{
					currentFolder = Path.GetDirectoryName(fileDialog.FileName);
					var json = await File.ReadAllTextAsync(fileDialog.FileName);
					var fights = JsonSerializer.Deserialize<List<Fight>>(json);
					SetupFights(fights);
					return;
				}
				if (folderDialog.ShowDialog().GetValueOrDefault())
				{
					string filePath = fileDialog.FileName;
					var names = await File.ReadAllLinesAsync(filePath);
					InitFights(names);
					currentFolder = folderDialog.FolderName;
					await TrySaveStateAsync(init: true);
				}
			}
		}

		private async void StartTimer(object sender, EventArgs e)
		{
			await TrySaveStateAsync();

			if (Fight.IsTimerStarted)
			{
				Fight.PauseTimer();
				BtnsColor = Color.DimGray;
			}
			else
			{
				Fight.StartTimer();
				BtnsColor = Color.Black;
			}
		}

		private void ResetTimer(object sender, EventArgs e)
		{
			DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
		}

		private void DecreaseBlueScore(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.BlueScore--;
			}
		}

		private void DecreaseRedScore(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
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

		private void OpenFightsList(object sender, RoutedEventArgs e)
		{
			if (!fightsListWindow.IsLoaded)
			{
				fightsListWindow = new(this);
				fightsListWindow.Show();
			}
			else
			{
				fightsListWindow.WindowState = WindowState.Normal;
				fightsListWindow.Focus();
			}
		}

		private async void DisplayFinishFightDialog(string cause, FinishCause finishCause)
		{
			await TrySaveStateAsync();
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
			var userChoice = MessageBox.Show(TextCollection.Finish,
				TextCollection.FightIsOver,
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (userChoice == MessageBoxResult.Yes)
			{
				Fight.IsCompleted = true;
				await TrySaveStateAsync();
				SetCurrentAndNextFights();
				userDeclines.Reset();
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

		private void OnEditFight(Fight? fight)
		{
			if (fight == null) return;

			fight.IsCompleted = false;
			Fight = fight;
			SetEnumerator(fight.RedName, fight.BlueName);
		}

		private async Task ExecuteGitCmdAsync(string folderPath, bool init = false)
		{
			string gitInint = init ? " && git init" : string.Empty;
			string command = $"cd /d \"{folderPath}\"{gitInint} && git add -A && git commit -m \"%date:~6,4%-%date:~3,2%-%date:~0,2% %time:~0,8%\"\r\n";

			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = "/c " + command; // /c — выполнить и закрыть
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;

			process.Start();

			string output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();
		}

		private void InitFights(IEnumerable<string> names)
		{
			var fights = FightsListFactory.CreateFights(
				names.Select(name => new Fighter { Name = name }).ToList(),
				new FightSettings()
				{
					DoubleHitsInARow = 5,
					DoubleHitsCommon = 5
				},
				new RelayCommand<Fight>(OnEditFight));
			SetupFights(fights);
		}

		private void SetupFights(IEnumerable<Fight> fights)
		{
			Fights.Clear();
			foreach (var fight in fights)
			{
				fight.OneDoubleHitLeft += isOneDoubleHitLeft => DoubleHitlLbl.Foreground = isOneDoubleHitLeft ? Brushes.Red : Brushes.Black;
				fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog(TextCollection.MaxDoubleHits, FinishCause.DoubleHits);
				fight.TimerTick += PlaySound;
				fight.TimerAlarms = ttmerAlarms;
				Fights.Add(fight);
			}

			enumerator = Fights.GetEnumerator();
			enumerator.MoveNext();
			SetCurrentAndNextFights();
		}

		public void SetEnumerator(string redName, string blueName)
		{
			enumerator = Fights.GetEnumerator();
			do
			{
				enumerator.MoveNext();
			}
			while (enumerator.Current.RedName != redName || enumerator.Current.BlueName != blueName);

			SetCurrentAndNextFights();
		}

		private async Task TrySaveStateAsync(bool init = false)
		{
			if (!string.IsNullOrWhiteSpace(currentFolder))
			{
				var fights = Fights.ToList();
				var json = JsonSerializer.Serialize(fights);
				await File.WriteAllTextAsync(Path.Combine(currentFolder, "Fights.json"), json);
				await ExecuteGitCmdAsync(currentFolder, init);
			}
		}

		private void SetCurrentAndNextFights()
		{
			mediaPlayers[0].Stop();
			mediaPlayers[1].Stop();
			mediaPlayers[0].Open(new Uri(@"C:\Vika\HemaCounter\HEMA\HEMA.Android\Resources\raw\beep.mp3", UriKind.Absolute));
			mediaPlayers[1].Open(new Uri(@"C:\Vika\HemaCounter\HEMA\HEMA.Android\Resources\raw\longBeep.mp3", UriKind.Absolute));

			if (enumerator.Current != null)
			{
				Fight = enumerator.Current;
				DoubleHitlLbl.Foreground = Brushes.Black;
			}

			do
			{
				enumerator.MoveNext();
			} while ((enumerator.Current?.IsCompleted).GetValueOrDefault());

			if (enumerator.Current != null && !enumerator.Current.IsCompleted)
			{
				NextFight = enumerator.Current;
			}
			else
			{
				NextFight = new(string.Empty, string.Empty, Fight.Settings);
			}
		}

		private void PlaySound()
		{
			if (fight.NextAlarm.HasValue && fight.Elapsed.TotalSeconds >= fight.NextAlarm.Value.TotalSeconds)
			{
				if (fight.NextAlarm.Value.PauseFight)
				{
					Fight.PauseTimer();
				}
				Application.Current.Dispatcher.Invoke(mediaPlayers[fight.NextAlarmIndex].Play);
				fight.NextAlarmIndex++;
			}
		}

	}
}