using System.ComponentModel;

namespace HEMA.WpfApp.Controls;

public static class DoubleEliminationBracketViewModelFactory
{
	private static Dictionary<string, Fight> fightsDictionary = new Dictionary<string, Fight>();
	private static DoubleEliminationBracket? currentBracket;

	public static DoubleEliminationBracketViewModel Create(
		DoubleEliminationBracket bracket,
		FightSettings fightSettings,
		Action<bool> oneDoubleHitLeftHandler)
	{
		currentBracket = bracket;

		if (bracket is null)
			return null!;

		var winners = CreateBracketViewModel(
			bracket.WinnersMatches.OrderBy(x => x.Round).ThenBy(x => x.Index).ToList(),
			"Финал верхней сетки",
			fightSettings,
			oneDoubleHitLeftHandler,
			isLoosers: false);

		var losers = CreateBracketViewModel(
			bracket.LosersMatches.OrderBy(x => x.Round).ThenBy(x => x.Index).ToList(),
			"Финал нижней сетки",
			fightSettings,
			oneDoubleHitLeftHandler,
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
				: ToFight(grandFinalNode, fightSettings, oneDoubleHitLeftHandler)
		};
	}

	private static BracketViewModel CreateBracketViewModel(
		List<MatchNode> matches,
		string finalTitle,
		FightSettings fightSettings,
		Action<bool> oneDoubleHitLeftHandler,
		bool isLoosers)
	{
		var groupedRounds = matches
			.GroupBy(x => x.Round)
			.OrderBy(x => x.Key)
			.Select(g => new RoundViewModel
			{
				Title = $"1/{GetTitle(g, isLoosers)}",
				Fights = g
					.OrderBy(x => x.Index)
					.Select(fight => ToFight(fight, fightSettings, oneDoubleHitLeftHandler))
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
				FinalMatch = groupedRounds[0].Fights.FirstOrDefault() ?? new Fight { Title = finalTitle }
			};
		}

		var finalRound = groupedRounds.Last();
		var finalMatch = finalRound.Fights.FirstOrDefault() ?? new Fight(string.Empty, string.Empty, fightSettings)
		{
			Title = finalTitle,
			BracketInfo = new()
			{
				BracketType = HEMA.BracketType.Final,
				BracketOrientation = BracketOrientation.Left,
			}
		};

		var roundsWithoutFinal = groupedRounds.Take(groupedRounds.Count - 1).ToList();

		int leftCount = roundsWithoutFinal.Count / 2;
		int rightCount = roundsWithoutFinal.Count - leftCount;

		var leftRounds = new List<RoundViewModel>(leftCount);
		var rightRounds = new List<RoundViewModel>(rightCount);
		for (int i = 0; i < roundsWithoutFinal.Count; i++)
		{
			var matchesCount = roundsWithoutFinal[i].Fights.Count / 2;
			leftRounds.Add(new()
			{
				Fights = roundsWithoutFinal[i].Fights
					.Select(fight => SetOrientation(fight, BracketOrientation.Left))
					.Take(matchesCount)
					.ToList(),
				Title = roundsWithoutFinal[i].Title,
			});
			rightRounds.Add(new()
			{
				Fights = roundsWithoutFinal[i].Fights
					.Skip(matchesCount)
					.Select(fight => SetOrientation(fight, BracketOrientation.Right))
					.ToList(),
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

	private static Fight SetOrientation(Fight fight, BracketOrientation bracketOrientation)
	{
		fight.BracketInfo.BracketOrientation = bracketOrientation;
		return fight;
	}

	private static int GetTitle(IGrouping<int, MatchNode> g, bool isLoosers)
		=> isLoosers ? g.Count() * 2 : g.Count();

	private static Fight ToFight(MatchNode match, FightSettings fightSettings, Action<bool> oneDoubleHitLeftHandler)
	{
		var fight = new Fight(GetSlotName(match.Slot1), GetSlotName(match.Slot2), fightSettings)
		{
			Title = GetMatchTitle(match),
			BracketInfo = new()
			{
				BracketId = match.Id,
				BracketType = match.Id.StartsWith("В") ? HEMA.BracketType.Winner 
								: match.Id.StartsWith("Н") ? HEMA.BracketType.Looser 
								: HEMA.BracketType.Final 
			},
			WinnerNextFightInfo = ToNextFightInfo(match.WinnerTo),
			LooserNextFightInfo = ToNextFightInfo(match.LoserTo),
		};
		SetupBracketFight(fight, fightSettings, oneDoubleHitLeftHandler);
		return fight;
	}

	public static Fight SetupBracketFight(Fight fight, FightSettings fightSettings, Action<bool> oneDoubleHitLeftHandler)
	{
		fight.OneDoubleHitLeft += oneDoubleHitLeftHandler;
		fight.PropertyChanged += FightCompleted;
		fightsDictionary.TryAdd(fight.Title, fight);
		return fight;
	}

	private static void FightCompleted(object sender, PropertyChangedEventArgs e)
	{
		var fight = (Fight)sender;
		if (e.PropertyName == nameof(fight.IsCompleted))
		{
			if (!fight.IsCompleted)
			{
				var match = currentBracket!.NodesDictionary[fight.BracketInfo.BracketId];
				fight.WinnerNextFightInfo = ToNextFightInfo(match.WinnerTo);
				fight.LooserNextFightInfo = ToNextFightInfo(match.LoserTo);
			}
			var winnerName = fight.RedScore > fight.BlueScore ? fight.RedName : fight.BlueName;
			var looserName = fight.BlueScore < fight.RedScore ? fight.BlueName : fight.RedName;

			if (fight.DoubleHits >= fight.MaxDoubleHits)
			{
				winnerName = looserName = "Проходной";
			}
			else if (fight.RedName == "Проходной")
			{
				winnerName = fight.BlueName;
				looserName = "Проходной";
			}
			else if (fight.BlueName == "Проходной")
			{
				winnerName = fight.RedName;
				looserName = "Проходной";
			}
			else if (fight.RedScore == fight.BlueScore)
			{
				return;
			}

			if (fight.WinnerNextFightInfo is not null)
			{
				fightsDictionary[fight.WinnerNextFightInfo.NextFightId].SetName(fight.WinnerNextFightInfo.NextFighterColor, winnerName);
			}
			if (fight.LooserNextFightInfo is not null)
			{
				fightsDictionary[fight.LooserNextFightInfo.NextFightId].SetName(fight.LooserNextFightInfo.NextFighterColor, looserName);
			}
		}
	}

	private static NextFightInfo? ToNextFightInfo(MatchLink? matchLink)
		=> matchLink is null ? null : new()
		{
			NextFighterColor = matchLink.FighterColor,
			NextFightId = matchLink.MatchId
		};

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
