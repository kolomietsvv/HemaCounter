namespace HEMA.WpfApp.Controls;

public static class DoubleEliminationBracketViewModelFactory
{
	public static DoubleEliminationBracketViewModel Create(DoubleEliminationBracket bracket)
	{
		if (bracket is null)
			return null!;

		var winners = CreateBracketViewModel(
			bracket.WinnersMatches.OrderBy(x => x.Round).ThenBy(x => x.Index).ToList(),
			"Финал верхней сетки",
			isLoosers: false);

		var losers = CreateBracketViewModel(
			bracket.LosersMatches.OrderBy(x => x.Round).ThenBy(x => x.Index).ToList(),
			"Финал нижней сетки",
			isLoosers: true);

		var grandFinalNode = bracket.GrandFinalMatches
			.OrderBy(x => x.Round)
			.ThenBy(x => x.Index)
			.FirstOrDefault();

		return new DoubleEliminationBracketViewModel
		{
			WinnersBracket = winners,
			LosersBracket = losers,
			GrandFinal = grandFinalNode is null
				? new Fight() { Title = "Финал" }
				: ToFight(grandFinalNode)
		};
	}

	private static BracketViewModel CreateBracketViewModel(
		List<MatchNode> matches,
		string finalTitle,
		bool isLoosers)
	{
		var groupedRounds = matches
			.GroupBy(x => x.Round)
			.OrderBy(x => x.Key)
			.Select(g => new RoundViewModel
			{
				Title = $"1/{GetTitle(g, isLoosers)}",
				Matches = g
					.OrderBy(x => x.Index)
					.Select(ToFight)
					.ToList()
			})
			.ToList();

		if (groupedRounds.Count == 0)
		{
			return new BracketViewModel
			{
				FinalTitle = finalTitle,
				FinalMatch = new Fight { Title = finalTitle }
			};
		}

		if (groupedRounds.Count == 1)
		{
			return new BracketViewModel
			{
				FinalTitle = finalTitle,
				FinalMatch = groupedRounds[0].Matches.FirstOrDefault() ?? new Fight { Title = finalTitle }
			};
		}

		var finalRound = groupedRounds.Last();
		var finalMatch = finalRound.Matches.FirstOrDefault() ?? new Fight { Title = finalTitle };

		var roundsWithoutFinal = groupedRounds.Take(groupedRounds.Count - 1).ToList();

		int leftCount = roundsWithoutFinal.Count / 2;
		int rightCount = roundsWithoutFinal.Count - leftCount;

		var leftRounds = new List<RoundViewModel>(leftCount);
		var rightRounds = new List<RoundViewModel>(rightCount);
		for (int i = 0; i < roundsWithoutFinal.Count; i++)
		{
			var matchesCount = roundsWithoutFinal[i].Matches.Count / 2;
			leftRounds.Add(new()
			{
				Matches = roundsWithoutFinal[i].Matches.Take(matchesCount).ToList(),
				Title = roundsWithoutFinal[i].Title,
			});
			rightRounds.Add(new()
			{
				Matches = roundsWithoutFinal[i].Matches.Skip(matchesCount).ToList(),
				Title = roundsWithoutFinal[i].Title,
			});
		}
		rightRounds.Reverse();
		return new BracketViewModel
		{
			LeftRounds = leftRounds,
			RightRounds = rightRounds,
			FinalTitle = finalTitle,
			FinalMatch = finalMatch
		};
	}

	private static int GetTitle(IGrouping<int, MatchNode> g, bool isLoosers)
		=> isLoosers ? g.Count() * 2 : g.Count();

	private static Fight ToFight(MatchNode match)
	{
		return new Fight(GetSlotName(match.Slot1), GetSlotName(match.Slot2), null!)
		{
			Title = GetMatchTitle(match),
		};
	}

	private static string GetMatchTitle(MatchNode match)
	{
		return match.Bracket switch
		{
			BracketType.Winners => $"Верх. {match.Round}.{match.Index}",
			BracketType.Losers => $"Ниж. {match.Round}.{match.Index}",
			BracketType.GrandFinal => $"Фин. {match.Round}.{match.Index}",
			_ => $"M {match.Round}.{match.Index}"
		};
	}

	private static string GetSlotName(SlotRef slot)
	{
		if (slot is null)
			return "—";

		return slot.Kind switch
		{
			SlotSourceKind.Fighter => slot.Fighter?.Name ?? "—",
			SlotSourceKind.Bye => "Проходной",
			SlotSourceKind.MatchWinner => $"Победитель {slot.FromMatchId}",
			SlotSourceKind.MatchLoser => $"Проигравший {slot.FromMatchId}",
			_ => "—"
		};
	}
}
