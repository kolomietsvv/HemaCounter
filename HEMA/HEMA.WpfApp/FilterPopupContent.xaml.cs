using System;
using System.Windows;
using System.Windows.Controls;

namespace HEMA.WpfApp.Controls
{
	public partial class FilterPopupContent : UserControl
	{
		public FilterPopupContent()
		{
			InitializeComponent();
		}

		public event EventHandler<string>? ApplyClicked;
		public event EventHandler? ResetClicked;

		public string FilterText
		{
			get => FilterNameBox.Text;
			set => FilterNameBox.Text = value;
		}

		public void FocusInput()
		{
			FilterNameBox.Focus();
			FilterNameBox.SelectAll();
		}

		public void Clear()
		{
			FilterNameBox.Text = string.Empty;
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			ApplyClicked?.Invoke(this, FilterNameBox.Text);
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			ResetClicked?.Invoke(this, EventArgs.Empty);
		}
	}
}