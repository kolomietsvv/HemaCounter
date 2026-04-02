using System.Windows;
using System.Windows.Controls;

namespace HEMA.WpfApp.Controls;

public partial class DoubleEliminationBracketControl : UserControl
{
	private Action<List<Fight>, string, string> _setCurrentFightAction;
	private Action _returnBackClick;
	public DoubleEliminationBracketViewModel BracketVM { get; set; }

	public DoubleEliminationBracketControl(
		List<Fighter> fighters,
		int participantsCount,
		Action<List<Fight>, string, string> setCurrentFightAction,
		FightSettings fightSettings,
		Action returnBackClick,
		Action<bool> oneDoubleHitLeftHandler)
	{
		ArgumentNullException.ThrowIfNull(fighters);

		InitializeComponent();

		var bracket = DoubleEliminationBracketGenerator.Generate(
			fighters,
			participantsCount,
			includeGrandFinalReset: false);

		BracketVM = DoubleEliminationBracketViewModelFactory.Create(bracket, fightSettings, oneDoubleHitLeftHandler);
		DataContext = BracketVM;
		_setCurrentFightAction = setCurrentFightAction;
		_returnBackClick = returnBackClick;
	}

	public DoubleEliminationBracketControl(
		DoubleEliminationBracketViewModel bracket,
		Action<List<Fight>, string, string> setCurrentFightAction,
		FightSettings fightSettings,
		Action returnBackClick,
		Action<bool> oneDoubleHitLeftHandler)
	{
		InitializeComponent();

		BracketVM = bracket;
		DataContext = BracketVM;
		_setCurrentFightAction = setCurrentFightAction;
		_returnBackClick = returnBackClick;
	}

	private void RunButton_Click(object sender, RoutedEventArgs e)
	{
		var fight = (Fight)((FrameworkElement)sender).DataContext;

		var fightsList = GetFights(GetBracket(fight), fight);

		foreach (var item in fightsList)
		{
			if (item.RedName == "Проходной" || item.BlueName == "Проходной")
			{
				item.IsCompleted = true;
			}
		}

		_setCurrentFightAction(fightsList, fight.RedName, fight.BlueName);
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
				return GetOrderedFights(bracket.LeftRounds);
			case BracketOrientation.Right:
				return GetOrderedFights(bracket.RightRounds);
		}
		return [fight];
	}

	private static List<Fight> GetOrderedFights(List<RoundViewModel> rounds)
	{
		return rounds
			.OrderByDescending(round => round.FightsCount)
			.ThenBy(round => round.Fights.FirstOrDefault()?.Title ?? "я")
			.SelectMany(round => round.Fights).ToList();
	}

	private void ReturnBack_Click(object sender, RoutedEventArgs e)
	{
		_returnBackClick();
	}
}