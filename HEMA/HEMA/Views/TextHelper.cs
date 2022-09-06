using Xamarin.Forms;

namespace HEMA.Views
{
    internal static class TextHelper
    {
        private static readonly char[] charsToTrim = new[] { '0', '-' };

        public static void RemoveExtraCharacters(object sender, TextChangedEventArgs e, int? maxValue = null, string stringFormat = null)
        {
            if (string.IsNullOrWhiteSpace(e.OldTextValue) || string.IsNullOrWhiteSpace(e.OldTextValue))
                return;

            var resultText = e.NewTextValue.Length > 1 && e.NewTextValue.StartsWith("0") ?
                e.NewTextValue.TrimStart(charsToTrim) : e.NewTextValue;
            resultText = resultText.Replace(".", string.Empty).Replace(",", string.Empty);

            if (string.IsNullOrWhiteSpace(resultText))
                resultText = "0";

            if (maxValue.HasValue && int.Parse(resultText) > maxValue)
                resultText = e.OldTextValue;

            if (!string.IsNullOrWhiteSpace(stringFormat))
            {
                resultText = int.Parse(resultText).ToString(stringFormat);
            }

            ((Entry)sender).Text = resultText;
        }
    }
}
