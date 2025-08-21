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

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var s = (value as string ?? "").Trim();

			// форматы: mm:ss или просто число секунд
			if (s.Contains(":"))
			{
				var parts = s.Split(':');
				if (parts.Length == 2 &&
					int.TryParse(parts[0], out var m) &&
					int.TryParse(parts[1], out var sec))
				{
					return new TimeSpan(0, m, sec);
				}
			}
			else if (int.TryParse(s, out var totalSec))
			{
				return TimeSpan.FromSeconds(totalSec);
			}

			return Binding.DoNothing;
		}
	}
}
