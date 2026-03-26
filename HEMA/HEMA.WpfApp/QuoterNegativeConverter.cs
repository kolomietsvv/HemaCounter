using System.Globalization;
using System.Windows.Data;

namespace HEMA.WpfApp;

public class QuoterNegativeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double d)
			return d * 3 / 4;

		return 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}