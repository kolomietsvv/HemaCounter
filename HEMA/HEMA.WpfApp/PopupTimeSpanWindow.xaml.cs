using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HEMA.WpfApp
{
	public partial class PopupTimeSpanWindow : Window, INotifyPropertyChanged, IDataErrorInfo
	{
		private TimeSpan _value = TimeSpan.Zero;
		private bool _hasError;
		private string _validationMessage;

		public TimeSpan Value
		{
			get => _value;
			set { _value = value; OnPropertyChanged(nameof(Value)); }
		}

		public bool HasError
		{
			get => _hasError;
			set { _hasError = value; OnPropertyChanged(nameof(HasError)); }
		}

		public string ValidationMessage
		{
			get => _validationMessage;
			set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }
		}

		public PopupTimeSpanWindow(TimeSpan? initial = null)
		{
			InitializeComponent();
			DataContext = this;
			if (initial.HasValue) Value = initial.Value;
			TimeBox.Focus();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.Focus();
			var wa = SystemParameters.WorkArea;
			Left = wa.Right - Width - 16;
			Top = wa.Bottom - Height - 16;
		}

		private void Plus10s_Click(object sender, RoutedEventArgs e) => Value = Value.Add(TimeSpan.FromSeconds(10));
		private void Minus10s_Click(object sender, RoutedEventArgs e) => Value = Value.Subtract(TimeSpan.FromSeconds(10));

		public string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Value))
				{
					if (Value < TimeSpan.Zero)
					{
						HasError = true; ValidationMessage = "Время не может быть отрицательным.";
						return ValidationMessage;
					}
					if (Value > TimeSpan.FromMinutes(59).Add(TimeSpan.FromSeconds(59)))
					{
						HasError = true; ValidationMessage = "Не более 59:59.";
						return ValidationMessage;
					}
				}
				HasError = false; ValidationMessage = null;
				return null;
			}
		}
		public string Error => null;

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			// проталкиваем текст в источник (запустит ConvertBack + валидацию)
			TimeBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)
				   ?.UpdateSource();

			if (HasError) return;
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (HasError) return;
			DialogResult = false;
			Close();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					Cancel_Click(sender, e);
					return;
				case Key.Enter:
					Ok_Click(sender, e);
					return;
				case Key.OemPlus:
				case Key.Add:
					Plus10s_Click(sender, e);
					e.Handled = true;
					return;
				case Key.OemMinus:
				case Key.Subtract:
					Minus10s_Click(sender, e);
					e.Handled = true;
					return;
			}
		}
	}
}
