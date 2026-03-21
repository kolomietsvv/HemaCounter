using System.Windows;
using System.Windows.Controls;

namespace HEMA.WpfApp.Controls;

public partial class DoubleEliminationBracketControl : UserControl
{
	private Action<List<Fight>, string, string> setCurrentFightAction;
	public DoubleEliminationBracketViewModel BracketVM { get; set; }

	public DoubleEliminationBracketControl(
		List<Fighter> fighters,
		int participantsCount,
		Action<List<Fight>, string, string> setCurrentFightAction,
		FightSettings fightSettings)
	{
		ArgumentNullException.ThrowIfNull(fighters);

		InitializeComponent();

		var bracket = DoubleEliminationBracketGenerator.Generate(
			fighters,
			participantsCount,
			includeGrandFinalReset: false);

		BracketVM = DoubleEliminationBracketViewModelFactory.Create(bracket, fightSettings);
		DataContext = BracketVM;
		this.setCurrentFightAction = setCurrentFightAction;
	}

	private void RunButton_Click(object sender, RoutedEventArgs e)
	{
		var fight = (Fight)((FrameworkElement)sender).DataContext;

		setCurrentFightAction(GetFights(GetBracket(fight), fight), fight.RedName, fight.BlueName);
	}

	private BracketViewModel GetBracket(Fight fight)
	{
		switch (fight.BracketInfo.BracketType)
		{
			case HEMA.BracketType.Winner:
				return BracketVM.WinnersBracket;
			case HEMA.BracketType.Looser:
				return BracketVM.LosersBracket;
			case HEMA.BracketType.Final:
				return new BracketViewModel
				{
					LeftRounds =
					[
						new RoundViewModel()
						{
							Fights = [BracketVM.GrandFinal]
						}
					]
				};
		}
		throw new InvalidOperationException();
	}

	private List<Fight> GetFights(BracketViewModel bracket, Fight fight)
	{
		switch (fight.BracketInfo.BracketOrientation)
		{
			case BracketOrientation.Left:
				return bracket.LeftRounds.SelectMany(round => round.Fights).ToList();
			case BracketOrientation.Right:
				return bracket.RightRounds.SelectMany(round => round.Fights).ToList();
		}
		throw new InvalidOperationException();
	}
}