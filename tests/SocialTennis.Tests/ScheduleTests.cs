using NUnit.Framework;
using SocialTennis.Players;
using SocialTennis.Scheduling;

namespace SocialTennis.Tests;

/// <summary>Independent checks of schedule invariants and setup rules.</summary>
[TestFixture]
public sealed class ScheduleTests
{
    /// <summary>Checks every practical session size and court count with several seeds.</summary>
    [Test]
    public void Generate_PlayerCourtMatrix_PreservesAllHardRules()
    {
        for (int count = 4; count <= 24; count++)
        {
            for (int courts = 1; courts <= 6; courts++)
            {
                for (int seed = 0; seed < 3; seed++)
                {
                    Player[] players = Players(count);
                    Round[] rounds = ScheduleEngine.Generate(players, courts, seed);
                    Check(players, courts, rounds, $"{count} players, {courts} courts, seed {seed}");
                }
            }
        }
    }

    /// <summary>Checks exact validation wording.</summary>
    [TestCase("", "Enter at least one court number, for example 4,5,6.")]
    [TestCase("4,x", "“x” is not a court number.")]
    [TestCase("4,", "“” is not a court number.")]
    [TestCase("0", "Court numbers must be 1 or more.")]
    [TestCase("-1", "Court numbers must be 1 or more.")]
    [TestCase("4,04", "Court 4 is listed more than once.")]
    [TestCase("2147483648", "“2147483648” is not a court number.")]
    public void Parse_InvalidCourts_ReturnsMessage(string input, string message)
    {
        Assert.That(CourtRules.Parse(input).Error, Is.EqualTo(message));
    }

    /// <summary>Checks numeric ordering and whitespace.</summary>
    [Test]
    public void Parse_UnorderedCourts_SortsNumerically()
    {
        Assert.That(CourtRules.Parse(" 10, 2, 4 ").Courts, Is.EqualTo([2, 4, 10]));
    }

    /// <summary>Checks reproducibility and input validation.</summary>
    [Test]
    public void Generate_SameSeed_RepeatsExactly()
    {
        Player[] players = Players(24);
        Assert.That(
            SessionJson.Serialize(new(1, ScheduleStep.Review, [1, 2, 3, 4, 5, 6], players, ScheduleEngine.Generate(players, 6, 42), false)),
            Is.EqualTo(SessionJson.Serialize(new(1, ScheduleStep.Review, [1, 2, 3, 4, 5, 6], players, ScheduleEngine.Generate(players, 6, 42), false))));
        Assert.Throws<ArgumentException>(() => ScheduleEngine.Generate(Players(3), 1, 0));
        Assert.Throws<ArgumentException>(() => ScheduleEngine.Generate(players, 0, 0));
        Assert.Throws<ArgumentException>(() => ScheduleEngine.Generate([players[0], players[0], players[1], players[2]], 1, 0));
    }

    /// <summary>Checks singles rank avoidance and necessary conflict marking.</summary>
    [TestCase(6, false)]
    [TestCase(10, false)]
    [TestCase(6, true)]
    public void Generate_SinglesRanks_AvoidsOrMinimizesConflicts(int count, bool lone)
    {
        Player[] players = [.. Players(count).Select((player, index) => player with { Rank = index < (lone ? 1 : count / 2) ? 1 : 3 })];
        Round[] rounds = ScheduleEngine.Generate(players, 6, 14);
        Check(players, 6, rounds, "Ranked singles");
        Game[] singles = [.. rounds.SelectMany(round => round.Games).Where(game => game.Left.Length + game.Right.Length == 2)];
        Assert.That(singles.Count(game => game.RankConflict), Is.EqualTo(lone ? 2 : 0));
        foreach (Game game in singles)
        {
            Assert.That(game.RankConflict, Is.EqualTo(players.Single(player => player.Id == game.Left[0]).Rank != players.Single(player => player.Id == game.Right[0]).Rank));
        }
    }

    internal static Player[] Players(int count)
    {
        return [.. Enumerable.Range(1, count).Select(index => new Player(new Guid(index, 0, 0, new byte[8]), $"Player{index}", "Tennis", null, string.Empty))];
    }

    private static void Check(Player[] players, int courts, Round[] rounds, string context)
    {
        Assert.That(rounds.Length, Is.EqualTo(players.Length switch { 4 => 3, 5 => 5, _ => 6 }), context);
        HashSet<(Guid, Guid)> partners = [];
        Dictionary<(Guid, Guid), int> opponents = [];
        Dictionary<Guid, int[]> shares = players.ToDictionary(player => player.Id, _ => new int[3]);
        foreach (Round round in rounds)
        {
            Guid[] present = [.. round.Games.SelectMany(game => game.Left.Concat(game.Right)), .. round.SittingOut];
            Assert.That(present, Is.EquivalentTo(players.Select(player => player.Id)), context);
            Assert.That(present.Distinct().Count(), Is.EqualTo(players.Length), context);
            int doubles = Math.Min(courts, players.Length / 4);
            int leftover = players.Length - (doubles * 4);
            int extra = doubles < courts && leftover is 2 or 3 ? leftover : 0;
            Assert.That(round.Games.Count(game => game.Left.Length == 2 && game.Right.Length == 2), Is.EqualTo(doubles), context);
            Assert.That(round.Games.Length, Is.EqualTo(doubles + (extra > 0 ? 1 : 0)), context);
            Assert.That(round.SittingOut.Length, Is.EqualTo(leftover - extra), context);
            Assert.That(round.Complete || round.Games.Any(game => game.Complete), Is.False, context);
            foreach (Guid id in round.SittingOut)
            {
                shares[id][0]++;
            }

            foreach (Game game in round.Games)
            {
                foreach (Guid[] team in new[] { game.Left, game.Right })
                {
                    if (team.Length == 2)
                    {
                        Assert.That(partners.Add(Pair(team[0], team[1])), Is.True, context + " repeated partner");
                    }
                }

                foreach (Guid left in game.Left)
                {
                    foreach (Guid right in game.Right)
                    {
                        (Guid, Guid) pair = Pair(left, right);
                        opponents[pair] = opponents.GetValueOrDefault(pair) + 1;
                        Assert.That(opponents[pair], Is.LessThanOrEqualTo(3), context + " opponent limit");
                    }
                }

                int size = game.Left.Length + game.Right.Length;
                if (size < 4)
                {
                    foreach (Guid id in game.Left.Concat(game.Right))
                    {
                        shares[id][size == 2 ? 1 : 2]++;
                    }
                }
            }
        }

        if (players.All(player => player.Rank is null) && players.Length != 6)
        {
            Assert.That(opponents.Values.Max(), Is.LessThanOrEqualTo(2), context + " unnecessary opponent fallback");
        }

        for (int kind = 0; kind < 3; kind++)
        {
            Assert.That(shares.Values.Max(value => value[kind]) - shares.Values.Min(value => value[kind]), Is.LessThanOrEqualTo(1), context + " unfair shares");
        }
    }

    private static (Guid First, Guid Second) Pair(Guid first, Guid second)
    {
        return first.CompareTo(second) < 0 ? (first, second) : (second, first);
    }
}
