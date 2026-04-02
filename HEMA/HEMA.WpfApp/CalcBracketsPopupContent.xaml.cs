using System.Windows;
using System.Windows.Controls;

namespace HEMA.WpfApp.Controls
{
	public partial class CalcBracketsPopupContent : UserControl
	{
		public CalcBracketsPopupContent()
		{
			InitializeComponent();
		}

		public event EventHandler<string>? ApplyClicked;
		public event EventHandler? ResetClicked;

		public int ParticipantcCount
		{
			get
			{
				int.TryParse(ParticipantCountBox.Text, out var intValue);
				return intValue;
			}
			set => ParticipantCountBox.Text = value.ToString();
		}

		public void FocusInput()
		{
			ParticipantCountBox.Focus();
			ParticipantCountBox.SelectAll();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			ApplyClicked?.Invoke(this, ParticipantCountBox.Text);
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			ResetClicked?.Invoke(this, EventArgs.Empty);
		}
	}
}