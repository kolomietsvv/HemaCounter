using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace HEMA.WpfApp
{
	/// <summary>
	/// Interaction logic for FightsListWindow.xaml
	/// </summary>
	public partial class RaitingWindow : Window
	{
		public MainWindow MainWindow { get; }

		public RaitingWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			MainWindow = mainWindow;
			DataContext = mainWindow;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				this.Close();
			}
		}
	}
}
