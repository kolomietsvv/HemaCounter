﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HEMA
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CommonSettingsPage : ContentPage
	{
		public CommonSettingsPage()
		{
			InitializeComponent();
		}

		private readonly char[] charsToTrim = new[] { '0' };

		private void RemoveExtraCharacters(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(e.OldTextValue) || string.IsNullOrWhiteSpace(e.OldTextValue))
				return;

			var resultText = e.OldTextValue == "0" ? e.NewTextValue.Trim(charsToTrim) : e.NewTextValue.TrimStart(charsToTrim);
			resultText = resultText.Replace(".", string.Empty).Replace(",", string.Empty);

			if (string.IsNullOrWhiteSpace(resultText))
				resultText = "0";

			((Entry)sender).Text = resultText;
		}
	}
}