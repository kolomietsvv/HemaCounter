using System.Windows;
using System.Windows.Input;

namespace HEMA.WpfApp
{
	public partial class DuplicateFightDialog : Window
	{
		public DuplicateFightDialogResult Result { get; private set; } = new()
		{
			Action = DuplicateFightAction.Ignore,
			ApplyToAll = false
		};

		public DuplicateFightDialog(string message)
		{
			InitializeComponent();
			MessageTextBlock.Text = message;
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			SetResultAndClose(DuplicateFightAction.Add);
		}

		private void ReplaceButton_Click(object sender, RoutedEventArgs e)
		{
			SetResultAndClose(DuplicateFightAction.Replace);
		}

		private void IgnoreButton_Click(object sender, RoutedEventArgs e)
		{
			SetResultAndClose(DuplicateFightAction.Ignore);
		}

		private void SetResultAndClose(DuplicateFightAction action)
		{
			Result = new DuplicateFightDialogResult
			{
				Action = action,
				ApplyToAll = ApplyToAllCheckBox.IsChecked == true
			};

			DialogResult = true;
			Close();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				SetResultAndClose(DuplicateFightAction.Ignore);
				e.Handled = true;
				return;
			}

			if (e.Key == Key.Enter)
			{
				SetResultAndClose(DuplicateFightAction.Add);
				e.Handled = true;
			}
		}
	}
}