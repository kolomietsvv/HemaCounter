using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using HEMA.WpfApp.Controls;

namespace HEMA.WpfApp
{
	/// <summary>
	/// Interaction logic for FightsListWindow.xaml
	/// </summary>
	public partial class RaitingWindow : Window
	{
		private object _defaultContent;
		public MainWindow MainWindow { get; }

		public RaitingWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			MainWindow = mainWindow;
			DataContext = mainWindow;
			_defaultContent = Content;
		}

		private void ShowBrackets_Click(object sender, RoutedEventArgs e)
		{
			Content = MainWindow.BracketControl;
		}

		private void CalculateBrackets_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.BracketControl = new DoubleEliminationBracketControl(
				MainWindow.Raiting.ToList(),
				26, MainWindow.SetEnumerator,
				MainWindow.GetSettings(),
				ReturnContentBack);
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Back:
					ReturnContentBack();
					return;
			}
		}

		private void ReturnContentBack()
		{
			Content = _defaultContent;
		}
	}
}
