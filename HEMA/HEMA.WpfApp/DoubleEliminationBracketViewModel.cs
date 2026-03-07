namespace HEMA.WpfApp;
public sealed class MatchViewModel
{
	public string Title { get; set; } = "";
	public Fighter? Fighter1 { get; set; }
	public Fighter? Fighter2 { get; set; }
}

public sealed class RoundViewModel
{
	public string Title { get; set; } = "";
	public List<MatchViewModel> Matches { get; set; } = new();
}

public sealed class BracketToCenterViewModel
{
	public List<RoundViewModel> LeftRounds { get; set; } = new();
	public List<RoundViewModel> RightRounds { get; set; } = new();

	public string FinalTitle { get; set; } = "Финал";
	public MatchViewModel? FinalMatch { get; set; }
}

public sealed class DoubleEliminationBracketViewModel
{
	public BracketToCenterViewModel WinnersBracket { get; set; } = new();
	public BracketToCenterViewModel LosersBracket { get; set; } = new();

	public MatchViewModel? GrandFinal { get; set; }
}