using HEMA.Common.Models;

namespace HEMA.WpfApp;

public enum BracketType
{
	Winners,
	Losers,
	GrandFinal
}

public enum SlotSourceKind
{
	None,
	Fighter,
	MatchWinner,
	MatchLoser,
	Bye
}

public sealed record SlotRef
{
	public SlotSourceKind Kind { get; init; }
	public Fighter? Fighter { get; init; }
	public string? FromMatchId { get; init; }

	public static SlotRef None() => new() { Kind = SlotSourceKind.None };
	public static SlotRef Bye() => new() { Kind = SlotSourceKind.Bye };
	public static SlotRef FighterRef(Fighter fighter) => new() { Kind = SlotSourceKind.Fighter, Fighter = fighter };
	public static SlotRef WinnerOf(string matchId) => new() { Kind = SlotSourceKind.MatchWinner, FromMatchId = matchId };
	public static SlotRef LoserOf(string matchId) => new() { Kind = SlotSourceKind.MatchLoser, FromMatchId = matchId };
}

public sealed record MatchLink(string MatchId, FighterColor FighterColor);

public sealed class MatchNode
{
	public required string Id { get; init; }
	public required BracketType Bracket { get; init; }
	public required int Round { get; init; }
	public required int Index { get; init; }

	public SlotRef Slot1 { get; set; } = SlotRef.None();
	public SlotRef Slot2 { get; set; } = SlotRef.None();

	public MatchLink? WinnerTo { get; set; }
	public MatchLink? LoserTo { get; set; }

	public override string ToString() => $"{Id}: {Bracket} {Round}.{Index}";
}

public sealed class DoubleEliminationBracket
{
	public required int ParticipantsCount { get; init; }
	public required int BracketSize { get; init; }
	public required int ByesCount { get; init; }
	public required int Height { get; init; }

	// Порядок seed-ов по листьям сетки.
	// Значение = 1-based номер посева.
	public required IReadOnlyList<int> SeedOrder { get; init; }

	// Для удобства: соответствие "номер посева -> боец"
	public required IReadOnlyDictionary<int, Fighter> SeedToFighter { get; init; }

	public required IReadOnlyList<MatchNode> Matches { get; init; }

	public required Dictionary<string, MatchNode> NodesDictionary { get; init; }

	public IReadOnlyList<MatchNode> WinnersMatches =>
		Matches.Where(x => x.Bracket == BracketType.Winners).ToList();

	public IReadOnlyList<MatchNode> LosersMatches =>
		Matches.Where(x => x.Bracket == BracketType.Losers).ToList();

	public IReadOnlyList<MatchNode> GrandFinalMatches =>
		Matches.Where(x => x.Bracket == BracketType.GrandFinal).ToList();
}

public static class DoubleEliminationBracketGenerator
{
	public static DoubleEliminationBracket Generate(
		List<Fighter> fighters,
		int participantsCount,
		bool includeGrandFinalReset = true)
	{
		if (fighters is null)
			return null!;

		if (participantsCount < 2)
			return null!;

		if (participantsCount > fighters.Count)
			return null!;

		// Берём только нужное число бойцов.
		// Предполагается, что список уже отсортирован по рейтингу убыв.
		var seededFighters = fighters.Take(participantsCount).ToList();

		// seed = позиция в списке + 1
		var seedToFighter = seededFighters
			.Select((fighter, index) => new { Seed = index + 1, Fighter = fighter })
			.ToDictionary(x => x.Seed, x => x.Fighter);

		int bracketSize = NextPowerOfTwo(participantsCount);
		int byesCount = bracketSize - participantsCount;
		int height = IntLog2(bracketSize);

		List<int> seedOrder = BuildCanonicalSeedOrder(bracketSize);
		var allMatches = new List<MatchNode>();

		var wb = new Dictionary<(int Round, int Index), MatchNode>();
		var lb = new Dictionary<(int Round, int Index), MatchNode>();

		// ---------- Winners Bracket ----------
		for (int t = 1; t <= height; t++)
		{
			int count = WinnersMatchesCount(bracketSize, t);

			for (int k = 1; k <= count; k++)
			{
				var node = new MatchNode
				{
					Id = $"Верх. {t}.{k}",
					Bracket = BracketType.Winners,
					Round = t,
					Index = k
				};

				wb[(t, k)] = node;
				allMatches.Add(node);
			}
		}

		// Первый раунд WB: раскладываем бойцов по листьям сетки через seedOrder
		for (int k = 1; k <= WinnersMatchesCount(bracketSize, 1); k++)
		{
			int leaf1 = 2 * k - 1;
			int leaf2 = 2 * k;

			wb[(1, k)].Slot1 = CreateInitialSlot(seedOrder[leaf1 - 1], participantsCount, seedToFighter);
			wb[(1, k)].Slot2 = CreateInitialSlot(seedOrder[leaf2 - 1], participantsCount, seedToFighter);
		}

		// Остальные раунды WB
		for (int t = 2; t <= height; t++)
		{
			int count = WinnersMatchesCount(bracketSize, t);

			for (int k = 1; k <= count; k++)
			{
				var left = wb[(t - 1, 2 * k - 1)];
				var right = wb[(t - 1, 2 * k)];

				wb[(t, k)].Slot1 = SlotRef.WinnerOf(left.Id);
				wb[(t, k)].Slot2 = SlotRef.WinnerOf(right.Id);
			}
		}

		// Winner links WB
		for (int t = 1; t < height; t++)
		{
			int count = WinnersMatchesCount(bracketSize, t);

			for (int k = 1; k <= count; k++)
			{
				int nextIndex = (k + 1) / 2;
				FighterColor nextSlot = k % 2 == 1 ? FighterColor.Red : FighterColor.Blue;

				wb[(t, k)].WinnerTo = new MatchLink(wb[(t + 1, nextIndex)].Id, nextSlot);
			}
		}

		// ---------- Losers Bracket ----------
		int losersRounds = 2 * height - 2;

		for (int s = 1; s <= losersRounds; s++)
		{
			int count = LosersMatchesCount(bracketSize, s);

			for (int k = 1; k <= count; k++)
			{
				var node = new MatchNode
				{
					Id = $"Ниж. {s}.{k}",
					Bracket = BracketType.Losers,
					Round = s,
					Index = k
				};

				lb[(s, k)] = node;
				allMatches.Add(node);
			}
		}

		// LB round 1: проигравшие соседних матчей WB round 1
		if (losersRounds >= 1)
		{
			int count = LosersMatchesCount(bracketSize, 1);

			for (int k = 1; k <= count; k++)
			{
				var wbLeft = wb[(1, 2 * k - 1)];
				var wbRight = wb[(1, 2 * k)];

				lb[(1, k)].Slot1 = SlotRef.LoserOf(wbLeft.Id);
				lb[(1, k)].Slot2 = SlotRef.LoserOf(wbRight.Id);

				wbLeft.LoserTo = new MatchLink(lb[(1, k)].Id, FighterColor.Red);
				wbRight.LoserTo = new MatchLink(lb[(1, k)].Id, FighterColor.Blue);
			}
		}

		// Далее minor/major rounds
		for (int t = 2; t <= height; t++)
		{
			int sMinor = 2 * t - 3;
			int sMajor = 2 * t - 2;

			// minor: схлопывание предыдущей волны LB
			if (sMinor >= 3)
			{
				int countMinor = LosersMatchesCount(bracketSize, sMinor);

				for (int k = 1; k <= countMinor; k++)
				{
					var prev1 = lb[(sMinor - 1, 2 * k - 1)];
					var prev2 = lb[(sMinor - 1, 2 * k)];

					lb[(sMinor, k)].Slot1 = SlotRef.WinnerOf(prev1.Id);
					lb[(sMinor, k)].Slot2 = SlotRef.WinnerOf(prev2.Id);

					prev1.WinnerTo = new MatchLink(lb[(sMinor, k)].Id, FighterColor.Red);
					prev2.WinnerTo = new MatchLink(lb[(sMinor, k)].Id, FighterColor.Blue);
				}
			}

			// major: победитель LB + проигравший из WB текущего раунда
			int countMajor = LosersMatchesCount(bracketSize, sMajor);

			for (int k = 1; k <= countMajor; k++)
			{
				MatchNode previousLbNode;

				if (sMajor == 2)
					previousLbNode = lb[(1, k)];
				else
					previousLbNode = lb[(sMinor, k)];

				int mappedWbIndex = PermuteForLosersInjection(WinnersMatchesCount(bracketSize, t), k);
				var wbLoserSource = wb[(t, mappedWbIndex)];

				lb[(sMajor, k)].Slot1 = SlotRef.WinnerOf(previousLbNode.Id);
				lb[(sMajor, k)].Slot2 = SlotRef.LoserOf(wbLoserSource.Id);

				previousLbNode.WinnerTo = new MatchLink(lb[(sMajor, k)].Id, FighterColor.Red);
				wbLoserSource.LoserTo = new MatchLink(lb[(sMajor, k)].Id, FighterColor.Blue);
			}
		}

		// odd -> even перенос победителей LB, если ещё не задан
		for (int s = 1; s < losersRounds; s += 2)
		{
			if (s + 1 <= losersRounds)
			{
				int count = LosersMatchesCount(bracketSize, s);
				int nextCount = LosersMatchesCount(bracketSize, s + 1);

				if (count == nextCount)
				{
					for (int k = 1; k <= count; k++)
					{
						lb[(s, k)].WinnerTo ??= new MatchLink(lb[(s + 1, k)].Id, FighterColor.Red);
					}
				}
			}
		}

		// ---------- Grand Finals ----------
		var wbChampion = wb[(height, 1)];
		var lbChampion = lb[(losersRounds, 1)];

		var gf1 = new MatchNode
		{
			Id = "Фин. 1.1",
			Bracket = BracketType.GrandFinal,
			Round = 1,
			Index = 1,
			Slot1 = SlotRef.WinnerOf(wbChampion.Id),
			Slot2 = SlotRef.WinnerOf(lbChampion.Id)
		};

		wbChampion.WinnerTo = new MatchLink(gf1.Id, FighterColor.Red);
		lbChampion.WinnerTo = new MatchLink(gf1.Id, FighterColor.Blue);

		allMatches.Add(gf1);

		if (includeGrandFinalReset)
		{
			var gf2 = new MatchNode
			{
				Id = "Фин 2",
				Bracket = BracketType.GrandFinal,
				Round = 2,
				Index = 1,
				Slot1 = SlotRef.WinnerOf(gf1.Id),
				Slot2 = SlotRef.LoserOf(gf1.Id)
			};

			gf1.WinnerTo = new MatchLink(gf2.Id, FighterColor.Red);
			gf1.LoserTo = new MatchLink(gf2.Id, FighterColor.Blue);

			allMatches.Add(gf2);
		}

		return new DoubleEliminationBracket
		{
			ParticipantsCount = participantsCount,
			BracketSize = bracketSize,
			ByesCount = byesCount,
			Height = height,
			SeedOrder = seedOrder,
			SeedToFighter = seedToFighter,
			Matches = allMatches,
			NodesDictionary = allMatches.ToDictionary(match => match.Id)
		};
	}

	private static SlotRef CreateInitialSlot(
		int seed,
		int participantsCount,
		IReadOnlyDictionary<int, Fighter> seedToFighter)
	{
		if (seed > participantsCount)
			return SlotRef.Bye();

		return SlotRef.FighterRef(seedToFighter[seed]);
	}

	private static int WinnersMatchesCount(int bracketSize, int round)
		=> bracketSize >> round;

	private static int LosersMatchesCount(int bracketSize, int round)
	{
		int exponent = ((round + 1) / 2) + 1;
		return bracketSize >> exponent;
	}

	private static int NextPowerOfTwo(int x)
	{
		if (x < 1)
			throw new ArgumentOutOfRangeException(nameof(x));

		int power = 1;
		while (power < x)
			power <<= 1;

		return power;
	}

	private static int IntLog2(int x)
	{
		if (x <= 0 || (x & (x - 1)) != 0)
			throw new ArgumentException("Число должно быть степенью двойки.", nameof(x));

		int result = 0;
		while ((x >>= 1) != 0)
			result++;

		return result;
	}

	/// <summary>
	/// Канонический порядок посева по листьям:
	/// [1]
	/// -> [1,2]
	/// -> [1,4,3,2]
	/// -> [1,8,4,5,3,6,2,7] - не матчевые пары, а порядок раскладки по листьям
	/// 
	/// Для дальнейшей разбивки по парам лучше использовать классическую "вставочную" схему.
	/// </summary>
	private static List<int> BuildCanonicalSeedOrder(int bracketSize)
	{
		var result = new List<int> { 1 };

		while (result.Count < bracketSize)
		{
			int newSize = result.Count * 2;
			var next = new List<int>(newSize);

			foreach (int x in result)
			{
				next.Add(x);
				next.Add(newSize + 1 - x);
			}

			result = next;
		}

		return result;
	}

	/// <summary>
	/// Перестановка для инъекции проигравших из WB в major-раунды LB.
	/// 1<->2, 3<->4, ...
	/// </summary>
	private static int PermuteForLosersInjection(int wbRoundMatchesCount, int k)
	{
		if (wbRoundMatchesCount == 1)
			return 1;

		return k % 2 == 1 ? k + 1 : k - 1;
	}
}