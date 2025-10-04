using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Security.Policy;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
		private const int MinAvailableScore = -2;

		public event PropertyChangedEventHandler? PropertyChanged;
		public RaitingWindow RaitingWindow { get; private set; }
		public ObservableCollection<Fight> Fights { get; }
		public ObservableCollection<Fighter> Raiting { get; }

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

		public MainWindow()
		{
			InitializeComponent();
			mediaPlayers = [new MediaPlayer(), new MediaPlayer()];

			Fights = new ObservableCollection<Fight>();
			Raiting = new ObservableCollection<Fighter>();
			InitFights(["Боец 1", "Боец 2", "Боец 3"], 3);

			DataContext = this;
			fightsListWindow = new FightsListWindow(this);
			RaitingWindow = new RaitingWindow(this);
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
					SetupFights(fights, new RelayCommand<Fight>(OnEditFight));
					return;
				}
				if (folderDialog.ShowDialog().GetValueOrDefault())
				{
					string filePath = fileDialog.FileName;
					var names = await File.ReadAllLinesAsync(filePath);
					InitFights(names, 3);
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
			}
			else
			{
				Fight.StartTimer();
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
				if (Fight.BlueScore <= MinAvailableScore)
				{
					BlueScoreLbl.Foreground = Brushes.Red;
					DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
				}
			}
		}

		private void DecreaseRedScore(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.RedScore--;
				if (Fight.RedScore <= MinAvailableScore)
				{
					RedScoreLbl.Foreground = Brushes.Red;
					DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
				}
			}
		}

		private void IncreaseBlueScore(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.BlueScore++;
				if (Fight.BlueScore > MinAvailableScore && BlueScoreLbl.Foreground == Brushes.Red)
				{
					BlueScoreLbl.Foreground = Brushes.White;
				}
			}
		}

		private void IncreaseRedScore(object sender, EventArgs e)
		{
			if (Fight.IsScoreChangeEnabled)
			{
				Fight.RedScore++;
				if (Fight.RedScore > MinAvailableScore && RedScoreLbl.Foreground == Brushes.Red)
				{
					RedScoreLbl.Foreground = Brushes.White;
				}
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
			if (Fight.RedScore > MinAvailableScore && RedScoreLbl.Foreground == Brushes.Red)
			{
				RedScoreLbl.Foreground = Brushes.White;
			}

		}

		private void IncreaseRedViolations(object sender, EventArgs e)
		{
			Fight.RedViolations++;
			if (Fight.RedScore <= MinAvailableScore)
			{
				RedScoreLbl.Foreground = Brushes.Red;
				DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
			}
		}

		private void DecreaseBlueViolations(object sender, EventArgs e)
		{
			Fight.BlueViolations--;
			if (Fight.BlueScore > MinAvailableScore && BlueScoreLbl.Foreground == Brushes.Red)
			{
				BlueScoreLbl.Foreground = Brushes.White;
			}
		}

		private void IncreaseBlueViolations(object sender, EventArgs e)
		{
			Fight.BlueViolations++;
			if (Fight.BlueScore <= MinAvailableScore)
			{
				BlueScoreLbl.Foreground = Brushes.Red;
				DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
			}
		}

		private void OpenFightsList(object sender, RoutedEventArgs e)
		{
			if (!fightsListWindow.IsLoaded)
			{
				OpenRaitingButton_Click(null!, null!);
				fightsListWindow = new(this);
				fightsListWindow.Show();
			}
			else
			{
				fightsListWindow.WindowState = WindowState.Normal;
				fightsListWindow.Focus();
			}
		}

		public void OpenRaitingButton_Click(object sender, RoutedEventArgs e)
		{
			var fighters = Fights
				.SelectMany<Fight, string>(fight => [fight.RedName, fight.BlueName])
				.Distinct()
				.Select(fighterName => new Fighter { Name = fighterName })
				.ToDictionary(fighter => fighter.Name);

			foreach (var fight in Fights)
			{
				fight.RedRaitingChange = "0";
				fight.BlueRaitingChange = "0";

				if (fight.RedScore <= MinAvailableScore)
				{
					fighters[fight.RedName].WinsCoefficient -= 2;
					fight.RedRaitingChange = "-2";
					if (fight.BlueScore > MinAvailableScore)
					{
						fighters[fight.BlueName].WinsCoefficient += 1;
						fight.BlueRaitingChange = "+1";
					}
					else
					{
						fighters[fight.BlueName].WinsCoefficient -= 2;
						fight.BlueRaitingChange = "-2";
					}
					continue;
				}
				if (fight.BlueScore <= MinAvailableScore)
				{
					fighters[fight.BlueName].WinsCoefficient -= 2;
					fight.BlueRaitingChange = "-2";
					if (fight.RedScore > MinAvailableScore)
					{
						fighters[fight.RedName].WinsCoefficient += 1;
						fight.RedRaitingChange = "+1";
					}
					else
					{
						fighters[fight.RedName].WinsCoefficient -= 2;
						fight.RedRaitingChange = "-2";
					}
					continue;
				}
				if (fight.DoubleHits >= fight.MaxDoubleHits)
				{
					fighters[fight.RedName].WinsCoefficient -= 2;
					fighters[fight.BlueName].WinsCoefficient -= 2;
					fight.RedRaitingChange = "-2";
					fight.BlueRaitingChange = "-2";
					continue;
				}
				if (fight.RedScore == 10 && fight.BlueScore == 0)
				{
					fighters[fight.RedName].WinsCoefficient += 2;
					fighters[fight.BlueName].WinsCoefficient -= 1;
					fight.RedRaitingChange = "+2";
					fight.BlueRaitingChange = "-1";
					continue;
				}
				if (fight.BlueScore == 10 && fight.RedScore == 0)
				{
					fighters[fight.BlueName].WinsCoefficient += 2;
					fighters[fight.RedName].WinsCoefficient -= 1;
					fight.BlueRaitingChange = "+2";
					fight.RedRaitingChange = "-1";
					continue;
				}
				if (fight.RedScore > fight.BlueScore)
				{
					fighters[fight.RedName].WinsCoefficient += 1;
					fighters[fight.BlueName].WinsCoefficient -= 1;
					fight.RedRaitingChange = "+1";
					fight.BlueRaitingChange = "-1";
					continue;
				}
				if (fight.BlueScore > fight.RedScore)
				{
					fighters[fight.BlueName].WinsCoefficient += 1;
					fighters[fight.RedName].WinsCoefficient -= 1;
					fight.BlueRaitingChange = "+1";
					fight.RedRaitingChange = "-1";
					continue;
				}
			}

			foreach (var fighter in fighters)
			{
				var fightsAsBlue = Fights.Where(fight => fight.BlueName == fighter.Key);
				var givenAsBlue = fightsAsBlue.Sum(fight => fight.BlueScore);
				var takenAsBlue = fightsAsBlue.Sum(fight => fight.RedScore);
				var fightsAsRed = Fights.Where(fight => fight.RedName == fighter.Key);
				var givenAsRed = fightsAsRed.Sum(fight => fight.RedScore);
				var takenAsRed = fightsAsRed.Sum(fight => fight.BlueScore);
				var doubleHitsAsBlue = fightsAsBlue.Sum(fight => fight.DoubleHits);
				var doubleHistAsRed = fightsAsRed.Sum(fight => fight.DoubleHits);
				var violationsAsBlue = fightsAsBlue.Sum(fight => fight.BlueViolations);
				var violationsAsRed = fightsAsRed.Sum(fight => fight.RedViolations);
				var elapsedAsBlue = fightsAsBlue.Sum(fight => fight.Elapsed.TotalMilliseconds);
				var elapsedAsRed = fightsAsRed.Sum(fight => fight.Elapsed.TotalMilliseconds);

				fighter.Value.GivenScore = givenAsBlue + givenAsRed;
				fighter.Value.TakenScore = takenAsBlue + takenAsRed;
				fighter.Value.DoubleHits = doubleHitsAsBlue + doubleHistAsRed;
				fighter.Value.Violations = violationsAsBlue + violationsAsRed;
				fighter.Value.Elapsed = elapsedAsBlue + elapsedAsRed;
			}

			Raiting.Clear();
			foreach (var fighter in fighters
				.Values
				.OrderByDescending(fighter => fighter.WinsCoefficient)
				.ThenByDescending(fighter => fighter.GivenTakenCoefficient)
				.ThenByDescending(fighter => fighter.GivenScore)
				.ThenBy(fighter => fighter.DoubleHits)
				.ThenBy(fighter => fighter.Violations)
				.ThenBy(fighter => fighter.Elapsed))
			{
				Raiting.Add(fighter);
			}

			if (sender != null)
			{
				if (!RaitingWindow.IsLoaded)
				{
					RaitingWindow = new(this);
					RaitingWindow.Show();
				}
				else
				{
					RaitingWindow.WindowState = WindowState.Normal;
					RaitingWindow.Focus();
				}
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
				OpenRaitingButton_Click(null!, null!);
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
			fightsListWindow.Close();
			Focus();
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

		private void InitFights(IEnumerable<string> names, int maxDoubleHits)
		{
			var fights = FightsListFactory.CreateFights(
				names.Select(name => new Fighter { Name = name }).ToList(),
				new FightSettings()
				{
					DoubleHitsInARow = maxDoubleHits,
					DoubleHitsCommon = maxDoubleHits
				});
			SetupFights(fights, new RelayCommand<Fight>(OnEditFight));
		}

		private void SetupFights(IEnumerable<Fight> fights, ICommand editFightCommand)
		{
			Fights.Clear();
			foreach (var fight in fights)
			{
				fight.OneDoubleHitLeft += isOneDoubleHitLeft => DoubleHitlLbl.Foreground = isOneDoubleHitLeft ? Brushes.Red : Brushes.Black;
				fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog(TextCollection.MaxDoubleHits, FinishCause.DoubleHits);
				fight.TimerTick += PlaySound;
				fight.TimerAlarms = ttmerAlarms;
				fight.EditFightCommand = editFightCommand;
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
				BlueScoreLbl.Foreground = Brushes.White;
				RedScoreLbl.Foreground = Brushes.White;
			}

			try
			{
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
			catch (Exception ex)
			{
				Console.WriteLine(ex);
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

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Q:
					IncreaseRedScore(sender, e);
					return;
				case Key.A:
					DecreaseRedScore(sender, e);
					return;
				case Key.OemCloseBrackets:
					IncreaseBlueScore(sender, e);
					return;
				case Key.OemQuotes:
					DecreaseBlueScore(sender, e);
					return;
				case Key.W:
					DecreaseBlueViolations(sender, e);
					return;
				case Key.S:
					DecreaseRedViolations(sender, e);
					return;
				case Key.X:
					DecreaseDoubleHits(sender, e);
					return;
				case Key.OemOpenBrackets:
					IncreaseBlueViolations(sender, e);
					return;
				case Key.OemSemicolon:
					IncreaseRedViolations(sender, e);
					return;
				case Key.OemPeriod:
					IncreaseDoubleHits(sender, e);
					return;
				case Key.Enter:
					DisplayFinishFightDialog(TextCollection.Ensure, FinishCause.Manual);
					return;
				case Key.Space:
					StartTimer(sender, e);
					return;
				case Key.T:
				case Key.N:
					ShowPopupTime_Click(sender, e);
					e.Handled = true;
					return;
				case Key.O:
				case Key.J:
					OpenFileButton_Click(sender, e);
					return;
				case Key.L:
					OpenRaitingButton_Click(sender, e);
					return;
				case Key.D:
				case Key.B:
					OpenFightsList(sender, e);
					return;

			}
		}
	}
}