using System.Globalization;
using System.Windows.Data;

namespace HEMA.WpfApp
{
	public class TimeSpanToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is TimeSpan ts)
				return ts.ToString(@"mm\:ss");
			return string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			Binding.DoNothing;
	}
}
