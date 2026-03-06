using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HEMA.WpfApp.Controls
{
	public partial class HostsListPopupContent : UserControl
	{
		public HostsListPopupContent()
		{
			InitializeComponent();
		}

		public event EventHandler? CancelClicked;
		public event EventHandler? ConnectClicked;

		public void FocusCard()
		{
			Keyboard.Focus(this);
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			CancelClicked?.Invoke(this, EventArgs.Empty);
		}

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			ConnectClicked?.Invoke(this, EventArgs.Empty);
		}

		private void HostsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ConnectClicked?.Invoke(this, EventArgs.Empty);
		}

		private void RootBorder_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe)
			{
				fe.Focus();
			}
		}

		private void RootBorder_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				CancelClicked?.Invoke(this, EventArgs.Empty);
				e.Handled = true;
				return;
			}

			if (e.Key == Key.Enter)
			{
				ConnectClicked?.Invoke(this, EventArgs.Empty);
				e.Handled = true;
			}
		}
	}
}