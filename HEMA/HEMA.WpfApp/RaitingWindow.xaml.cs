using System.Windows;
using System.Windows.Input;

using HEMA.WpfApp.Controls;

namespace HEMA.WpfApp
{
	/// <summary>
	/// Interaction logic for FightsListWindow.xaml
	/// </summary>
	public partial class RaitingWindow : Window
	{
		private bool bracketsCalculated;
		private object _defaultContent;
		public MainWindow MainWindow { get; }

		public RaitingWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			MainWindow = mainWindow;
			DataContext = mainWindow;
			_defaultContent = Content;

			CalcBracketsPopupContent.ApplyClicked += CalcBracketsPopup_ApplyClicked;
			CalcBracketsPopupContent.ResetClicked += CalcBracketPopup_CancledClicked;
		}

		private void CalcBracketPopup_CancledClicked(object? sender, EventArgs e)
		{
			CalcBracketsPopup.IsOpen = false;
		}

		private void CalcBracketsPopup_ApplyClicked(object? sender, string e)
		{
			var count = CalcBracketsPopupContent.ParticipantcCount;
			if (count <= 4)
			{

			}
			MainWindow.BracketControl = new DoubleEliminationBracketControl(
				MainWindow.Raiting.ToList(),
				count, MainWindow.SetEnumerator,
				MainWindow.GetSettings(),
				ReturnContentBack,
				MainWindow.ChangeDoubleHitColor);
			bracketsCalculated = true;
			Content = MainWindow.BracketControl;
		}

		private void ShowBrackets_Click(object sender, RoutedEventArgs e)
		{
			if (bracketsCalculated)
				Content = MainWindow.BracketControl;
			else
				CalculateBrackets_Click(sender, e);
		}

		private void CalculateBrackets_Click(object sender, RoutedEventArgs e)
		{
			if (!CalcBracketsPopup.IsOpen)
			{
				CalcBracketsPopup.IsOpen = true;
				CalcBracketsPopupContent.FocusInput();
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Back:
				case Key.Tab:
					if (Content == _defaultContent)
						ShowBrackets_Click(sender, e);
					else
						ReturnContentBack();
					return;
				case Key.Escape:
					if (CalcBracketsPopup.IsOpen)
					{
						CalcBracketsPopup.IsOpen = false;
						return;
					}
					Hide();
					return;
				case Key.Enter:
					if (CalcBracketsPopup.IsOpen)
					{
						CalcBracketsPopup_ApplyClicked(sender, CalcBracketsPopupContent.ParticipantCountBox.Text);
					}
					return;
				case Key.C:
					e.Handled = true;
					CalculateBrackets_Click(sender, e);
					return;
			}
		}

		private void ReturnContentBack()
		{
			Content = _defaultContent;
		}
	}
}
