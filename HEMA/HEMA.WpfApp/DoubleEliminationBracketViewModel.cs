namespace HEMA.WpfApp.Controls

{
	public sealed class RoundViewModel
	{
		public string Title { get; set; } = "";
		public List<Fight> Fights { get; set; } = new();
	}

	public sealed class BracketViewModel
	{
		public List<RoundViewModel> LeftRounds { get; set; } = new();
		public List<RoundViewModel> RightRounds { get; set; } = new();

		public string FinalTitle { get; set; } = "Финал";
		public Fight? FinalMatch { get; set; }
	}

	public sealed class DoubleEliminationBracketViewModel
	{
		public BracketViewModel WinnersBracket { get; set; } = new();
		public BracketViewModel LosersBracket { get; set; } = new();

		public Fight? GrandFinal { get; set; }
	}
}