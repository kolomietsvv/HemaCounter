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

		public int GivenTakenCoefficient => GivenScore - TakenScore;

		public int GivenScore { get; set; }

		public int DoubleHits { get; set; }

		public int Violations { get; set; }

		public double Elapsed { get; set; }

		public TimeSpan Time => TimeSpan.FromMilliseconds(Elapsed);

		public int TakenScore { get; set; }
	}
}
