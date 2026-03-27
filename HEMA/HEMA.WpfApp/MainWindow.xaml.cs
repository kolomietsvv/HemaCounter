using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using HEMA.Models;
using HEMA.WpfApp.Controls;

using Microsoft.Win32;


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
		private int maxDoubleHits = 5;
		private bool gitExists;
		private MediaPlayer[] mediaPlayers;
		private TimerAlarmLight[] ttmerAlarms =
			[
				new TimerAlarmLight { TotalSeconds = 105},
				new TimerAlarmLight { TotalSeconds = 120, PauseFight = true }
			];
		private Task hostTask;
		private const int MinAvailableScore = -100;

		public event PropertyChangedEventHandler? PropertyChanged;
		public RaitingWindow RaitingWindow { get; private set; }
		public ObservableCollection<Fight> Fights { get; }
		public ObservableCollection<Fighter> Raiting { get; }
		public ObservableCollection<HostInfo> DiscoveredHosts { get; } = new();
		public HostInfo? SelectedHost { get; set; }
		public TcpService TcpService { get; }
		public FightSettings FightSettings { get; private set; }

		public DoubleEliminationBracketControl BracketControl { get; set; }


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
			InitFights(["Боец 1", "Боец 2", "Боец 3", "Боец 4", "Боец 5",
				"Боец 6", "Боец 7", "Боец 8", "Боец 9", "Боец 10", "Боец 11", "Боец 12",
				"Боец 13", "Боец 14", "Боец 15", "Боец 16", "Боец 17", "Боец 18", "Боец 19",
				"Боец 20", "Боец 21", "Боец 22", "Боец 23", "Боец 24", "Боец 25", "Боец 26"]);

			DataContext = this;
			fightsListWindow = new FightsListWindow(this);
			RaitingWindow = new RaitingWindow(this);

			gitExists = DefineGitExists();

			TcpService = new TcpService();
			TcpService.Start(
				fightsListWindow.HandleFightsRequest,
				fightsListWindow.AcceptFights);
			WindowState = WindowState.Maximized;
		}

		public string GetSerializedFights()
		{
			var fights = Fights.ToList();
			var saveStruct = new SaveStruct
			{
				Fights = fights,
				BracketsVM = BracketControl.BracketVM,
			};
			var json = JsonSerializer.Serialize(saveStruct);
			return json;
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
					mediaPlayers[0].Open(new Uri(@"raw\beep.mp3", UriKind.Relative));
				}
				else if (popup.Value.TotalSeconds <= ttmerAlarms[0].TotalSeconds)
				{
					Fight.NextAlarmIndex = 1;
					mediaPlayers[1].Stop();
					mediaPlayers[1].Open(new Uri(@"raw\longBeep.mp3", UriKind.Relative));
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
					var saveStruct = JsonSerializer.Deserialize<SaveStruct>(json);
					SetupFights(saveStruct, new RelayCommand<Fight>(OnEditFight));
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

				if (fight.DoubleHits >= fight.MaxDoubleHits)
				{
					fighters[fight.RedName].WinsCoefficient -= 2;
					fighters[fight.BlueName].WinsCoefficient -= 2;
					fight.RedRaitingChange = "-2";
					fight.BlueRaitingChange = "-2";
					continue;
				}
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
				int fightsCount = Fights.Count(f => f.RedName == fighter.Key || f.BlueName == fighter.Key);

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

				fighter.Value.MaxPossibleWinsCoefficient = fightsCount * 2;
				fighter.Value.MaxPosiibleGivenTakenCoefficient = fightsCount * 10;
				fighter.Value.MaxPossibleGivenScore = fightsCount * 10;
				fighter.Value.MaxDoubleHits = fightsCount * 3;

				fighter.Value.GivenScore = givenAsBlue + givenAsRed;
				fighter.Value.TakenScore = takenAsBlue + takenAsRed;
				fighter.Value.DoubleHits = doubleHitsAsBlue + doubleHistAsRed;
				fighter.Value.Violations = violationsAsBlue + violationsAsRed;
				fighter.Value.Elapsed = elapsedAsBlue + elapsedAsRed;
			}

			Raiting.Clear();
			foreach (var fighter in fighters
				.Values
				.OrderByDescending(fighter => fighter.WinsCoefficientCalculated)
				.ThenByDescending(fighter => fighter.GivenTakenCoefficientCalculated)
				.ThenByDescending(fighter => fighter.GivenScoreCalculated)
				.ThenBy(fighter => fighter.DoubleHitsCalculated)
				.ThenBy(fighter => fighter.Violations)
				.ThenBy(fighter => fighter.Elapsed))
			{
				Raiting.Add(fighter);
			}

			File.WriteAllLines(
				"Fights.csv",
				// Заголовок
				new[] { "№;Имя (Red);Нанес (Red);Время;Нанес (Blue);Имя (Blue);Обоюдн.;Предупр. (Red);Предупр. (Blue);Номинация;Подгруппа" }
				.Concat(
					Fights.Select(fight =>
						$"{fight.OriginalIndex};" +
						$"{fight.RedName};" +
						$"{fight.RedScore};" +
						$"{fight.Elapsed};" +
						$"{fight.BlueScore};" +
						$"{fight.BlueName};" +
						$"{fight.DoubleHits};" +
						$"{fight.RedViolations};" +
						$"{fight.BlueViolations};" +
						$"{fight.NominationName};" +
						$"{fight.SubgroupName}"
					)
				)
			);

			// Экспорт массива fighters (или коллекции Raiting) в CSV
			File.WriteAllLines(
				"FightersRaiting.csv",
				// Заголовок
				new[] { "Имя;Коэф. побед;Нанес. - пропущ.;Нанес.;Обоюд.;Предупр.;Время" }
				.Concat(
					Raiting.Select(fighter =>
						$"{fighter.Name};" +
						$"{fighter.WinsCoefficientDisplay};" +
						$"{fighter.GivenTakenCoefficient};" +
						$"{fighter.GivenScoreDisplay};" +
						$"{fighter.DoubleHitsDisplay};" +
						$"{fighter.Violations};" +
						$"{fighter.Time}"
					)
				)
			);


			if (sender != null)
			{
				if (!RaitingWindow.IsLoaded)
				{
					var bracketsCalculated = RaitingWindow?.BracketsCalculated;
					RaitingWindow = new(this) { BracketsCalculated = bracketsCalculated.GetValueOrDefault() };
					RaitingWindow.Closing += (s, e) => { ((RaitingWindow)s).Hide(); e.Cancel = true; };
					RaitingWindow.Show();
				}
				else
				{
					RaitingWindow.Show();
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
			SetEnumerator(Fights, fight.RedName, fight.BlueName);
			fightsListWindow.Close();
			Focus();
		}

		private async Task ExecuteGitCmdAsync(string folderPath, bool init = false)
		{
			string gitInint = init ? " && git init" : string.Empty;
			string command = $"cd /d \"{folderPath}\"{gitInint} && git add -A && git commit -m \"%date:~6,4%-%date:~3,2%-%date:~0,2% %time:~0,8%\"\r\n";

			Process process = ExecuteCmd(command);

			string output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();
		}

		private Task SaveToHistoryFolderAsync(string folderPath, bool init = false)
		{
			var dateTimeNow = DateTime.Now;
			const string historyFolderName = "history";
			if (init)
			{
				Directory.CreateDirectory(Path.Combine(folderPath, historyFolderName));
			}
			string json = GetSerializedFights();
			return File.WriteAllTextAsync(
				Path.Combine(folderPath, historyFolderName, dateTimeNow.ToString("HH_mm_ss")),
				json);
		}

		private void InitFights(IEnumerable<string> names)
		{
			var fights = FightsListFactory.CreateFights(
				names.Select(name => new Fighter { Name = name }).ToList(),
				GetSettings());
			SetupFights(new SaveStruct { Fights = fights.ToList() }, new RelayCommand<Fight>(OnEditFight));
		}

		public FightSettings GetSettings()
		{
			return new FightSettings()
			{
				DoubleHitsInARow = maxDoubleHits,
				DoubleHitsCommon = maxDoubleHits
			};
		}

		private void SetupFights(SaveStruct saveStruct, ICommand editFightCommand)
		{
			Fights.Clear();

			foreach (var fight in saveStruct.Fights)
			{
				fight.OneDoubleHitLeft += ChangeDoubleHitColor;
				fight.MaxDoubleHitsReached += () => DisplayFinishFightDialog(TextCollection.MaxDoubleHits, FinishCause.DoubleHits);
				fight.TimerTick += PlaySound;
				fight.TimerAlarms = ttmerAlarms;
				fight.EditFightCommand = editFightCommand;
				Fights.Add(fight);
			}
			if (saveStruct.BracketsVM is not null)
			{
				if(RaitingWindow.BracketsCalculated)
				{
					UpdateNotCompletedFights(saveStruct);
				}
				SetupBracketsFight(saveStruct);
				BracketControl = new DoubleEliminationBracketControl(
					saveStruct.BracketsVM, SetEnumerator,
					GetSettings(),
					RaitingWindow.ReturnContentBack,
					ChangeDoubleHitColor);
				RaitingWindow.BracketsCalculated = true;
				RaitingWindow.Closing += (s, e) => { ((RaitingWindow)s).Hide(); e.Cancel = true; };
				RaitingWindow.Activate();
				RaitingWindow.Hide();
			}

			SetupEnumerator(Fights);
		}

		private void SetupBracketsFight(SaveStruct saveStruct)
		{
			foreach (var item in saveStruct.BracketsVM.WinnersBracket.RightRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
			foreach (var item in saveStruct.BracketsVM.WinnersBracket.LeftRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
			{
				var item = saveStruct.BracketsVM.WinnersBracket.FinalMatch;
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
			foreach (var item in saveStruct.BracketsVM.LosersBracket.RightRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
			foreach (var item in saveStruct.BracketsVM.LosersBracket.LeftRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
			{
				var item = saveStruct.BracketsVM.LosersBracket.FinalMatch;
				DoubleEliminationBracketViewModelFactory.SetupBracketFight(item, item.Settings, ChangeDoubleHitColor);
			}
		}

		private void UpdateNotCompletedFights(SaveStruct saveStruct)
		{
			foreach (var item in saveStruct.BracketsVM.WinnersBracket.RightRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
			foreach (var item in saveStruct.BracketsVM.WinnersBracket.LeftRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
			{
				var item = saveStruct.BracketsVM.WinnersBracket.FinalMatch;
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
			foreach (var item in saveStruct.BracketsVM.LosersBracket.RightRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
			foreach (var item in saveStruct.BracketsVM.LosersBracket.LeftRounds.SelectMany(round => round.Fights))
			{
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
			{
				var item = saveStruct.BracketsVM.LosersBracket.FinalMatch;
				DoubleEliminationBracketViewModelFactory.UpdateFightIfCompleted(item);
			}
		}


		public void ChangeDoubleHitColor(bool isOneDoubleHitLeft)
		{
			DoubleHitlLbl.Foreground = isOneDoubleHitLeft ? Brushes.Red : Brushes.Black;
		}

		public void SetupEnumerator(IEnumerable<Fight> fights)
		{
			enumerator = fights.GetEnumerator();
			enumerator.MoveNext();
			SetCurrentAndNextFights();
		}

		public void SetEnumerator(IEnumerable<Fight> fights, string redName, string blueName)
		{
			enumerator = fights.GetEnumerator();
			do
			{
				enumerator.MoveNext();
			}
			while (enumerator.Current.RedName != redName || enumerator.Current.BlueName != blueName);

			SetCurrentAndNextFights();
			Focus();
		}

		private async Task TrySaveStateAsync(bool init = false)
		{
			if (!string.IsNullOrWhiteSpace(currentFolder))
			{
				string commonFightsJson = GetSerializedFights();
				await File.WriteAllTextAsync(Path.Combine(currentFolder, "Fights.json"), commonFightsJson);
				if (gitExists)
				{
					await ExecuteGitCmdAsync(currentFolder, init);
				}
				else
				{
					await SaveToHistoryFolderAsync(currentFolder, init);
				}
			}
		}

		private static bool DefineGitExists()
		{
			string command = "git version";

			Process process = ExecuteCmd(command);

			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			return output.StartsWith(command);
		}

		private static Process ExecuteCmd(string command)
		{
			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = "/c " + command; // /c — выполнить и закрыть
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			return process;
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