using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using HEMA.Models;

namespace HEMA
{
	public class Fighter
	{
		public string Name { get; set; }

		public int WinsCoefficient { get; set; }

		public double MaxPossibleWinsCoefficient { get; set; }

		public double WinsCoefficientCalculated => WinsCoefficient / MaxPossibleWinsCoefficient;

		public string WinsCoefficientDisplay => $"{WinsCoefficientCalculated:F2} ({WinsCoefficient}/{MaxPossibleWinsCoefficient:F0})";

		public int GivenTakenCoefficient => GivenScore - TakenScore;

		public double MaxPosiibleGivenTakenCoefficient { get; set; }

		public double GivenTakenCoefficientCalculated => GivenTakenCoefficient / MaxPosiibleGivenTakenCoefficient;

		public string GivenTakenDisplay => $"{GivenTakenCoefficientCalculated:F2} ({GivenTakenCoefficient}/{MaxPosiibleGivenTakenCoefficient:F0})";

		public int GivenScore { get; set; }

		public double MaxPossibleGivenScore { get; set; }

		public double GivenScoreCalculated => GivenScore / MaxPossibleGivenScore;

		public string GivenScoreDisplay => $"{GivenScoreCalculated:F2} ({GivenScore}/{MaxPossibleGivenScore:F0})";

		public int DoubleHits { get; set; }

		public double MaxDoubleHits { get; set; }

		public double DoubleHitsCalculated => DoubleHits / MaxDoubleHits;

		public string DoubleHitsDisplay => $"{DoubleHitsCalculated:F2} ({DoubleHits}/{MaxDoubleHits:F0})";

		public int Violations { get; set; }

		public double Elapsed { get; set; }

		public TimeSpan Time => TimeSpan.FromMilliseconds(Elapsed);

		public int TakenScore { get; set; }
	}
}
