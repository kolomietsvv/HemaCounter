using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using HEMA.Models;

namespace HEMA
{
	public class FightsListFactory
	{
		public static IEnumerable<Fight> CreateFights(List<Fighter> fighters, FightSettings settings)
		{
			if (fighters.Count == 0)
			{
				return new List<Fight>();
			}

			var rounds = BuildRounds(fighters.Select(fighter => fighter.Name).ToList());
			var schedule = FlattenRoundsAvoidBackToBack(rounds);
			int i = 0;
			return schedule
				.Select(pair => new Fight(
					pair.Item1,
					pair.Item2,
					settings)
				{
					OriginalIndex = schedule.Count - i++
				})
				.OrderBy(fight => fight.OriginalIndex);
		}

		/// <summary>
		/// Строит пары боёв по методу кругов. 
		/// Если N нечётное — добавит фиктивного "BYE".
		/// Возвращает список туров; внутри тура каждый боец дерётся не более одного раза.
		/// </summary>
		public static List<List<(string A, string B)>> BuildRounds(IList<string> fighters)
		{
			var F = fighters.ToList();
			var hadBye = false;
			if (F.Count % 2 == 1)
			{
				F.Add("BYE");
				hadBye = true;
			}

			int n = F.Count;
			var fixedOne = F[0];
			var rest = F.Skip(1).ToList();
			var rounds = new List<List<(string A, string B)>>();

			for (int r = 0; r < n - 1; r++)
			{
				var circle = new List<string> { fixedOne };
				circle.AddRange(rest);

				var pairs = new List<(string A, string B)>();
				for (int i = 0; i < n / 2; i++)
				{
					var a = circle[i];
					var b = circle[^(i + 1)];
					if (a != "BYE" && b != "BYE")
						pairs.Add((a, b));
				}
				rounds.Add(pairs);

				// вращение (все, кроме фиксированного)
				// последний элемент rest идёт в начало
				if (rest.Count > 0)
				{
					var last = rest[^1];
					rest.RemoveAt(rest.Count - 1);
					rest.Insert(0, last);
				}
			}

			return rounds;
		}

		/// <summary>
		/// Преобразует туры в плоский список боёв, стараясь избежать стыков «подряд» между турами.
		/// Внутри тура порядок пар может быть переупорядочен.
		/// </summary>
		public static List<(string A, string B)> FlattenRoundsAvoidBackToBack(List<List<(string A, string B)>> rounds)
		{
			var schedule = new List<(string A, string B)>();
			var lastFight = new HashSet<string>();

			foreach (var round in rounds)
			{
				// лёгкая перестановка внутри тура, чтобы не ставить тех,
				// кто только что дрался, в начало
				var sorted = round
					.OrderBy(p => OverlapCount(p, lastFight))
					.ToList();

				schedule.AddRange(sorted);
				if (sorted.Count > 0)
					lastFight = new HashSet<string> { sorted[^1].A, sorted[^1].B };
			}

			return schedule;

			static int OverlapCount((string A, string B) p, HashSet<string> last)
				=> (last.Contains(p.A) ? 1 : 0) + (last.Contains(p.B) ? 1 : 0);
		}
	}
}
