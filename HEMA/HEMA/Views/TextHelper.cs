using Xamarin.Forms;

namespace HEMA.Views
{
    internal static class TextHelper
    {
        private static readonly char[] charsToTrim = new[] { '0', '-' };

        public static void RemoveExtraCharacters(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.OldTextValue) || string.IsNullOrWhiteSpace(e.OldTextValue))
                return;

            var resultText = e.NewTextValue.Length > 1 && e.NewTextValue.StartsWith("0") ?
                e.NewTextValue.TrimStart(charsToTrim) : e.NewTextValue;
            resultText = resultText.Replace(".", string.Empty).Replace(",", string.Empty);

            if (string.IsNullOrWhiteSpace(resultText))
                resultText = "0";

            ((Entry)sender).Text = resultText;
        }
    }
}
