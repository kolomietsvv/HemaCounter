using System.Globalization;
using System.Windows.Data;

namespace HEMA.WpfApp
{
	public class InverseBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool b)
				return !b;
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool b)
				return !b;
			return Binding.DoNothing;
		}
	}
}
