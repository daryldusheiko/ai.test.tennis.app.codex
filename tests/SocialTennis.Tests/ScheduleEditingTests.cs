using NUnit.Framework;
using SocialTennis.Players;
using SocialTennis.Scheduling;

namespace SocialTennis.Tests;

/// <summary>Exercises editing, constrained repair and backwards-compatible persistence.</summary>
[TestFixture]
public sealed class ScheduleEditingTests
{
    /// <summary>Checks exact swap diagnostics and immutable failures.</summary>
    [Test]
    public void Swap_InvalidTargets_ReturnsMessagesWithoutChanges()
    {
        Session session = Create(8, 2);
        Guid[] side = session.Rounds[0].Games[0].Left;
        var first = new PlayerPosition(0, side[0]);
        Assert.That(ScheduleEditing.SwapError(session, first, new(1, side[1])), Is.EqualTo("Pick a player in round 1."));
        Assert.That(ScheduleEditing.SwapError(session, first, new(0, side[1])), Does.Contain("are already on the same side."));
        Session pinned = ScheduleEditing.Pin(session, first);
        Assert.That(ScheduleEditing.SwapError(pinned, first, new(0, session.Rounds[0].Games[0].Right[0])), Does.Contain("is pinned and can't be swapped."));
        Assert.That(ScheduleEditing.Swap(pinned, first, new(0, side[1])), Is.SameAs(pinned));
        Assert.That(ScheduleEditing.Pin(pinned, first).Rounds[0].Pinned, Is.Empty);
        Session complete = ScheduleEditing.Replace(session, 0, session.Rounds[0] with { Complete = true });
        Assert.That(ScheduleEditing.Pin(complete, first), Is.SameAs(complete));
        Assert.That(ScheduleEditing.SwapError(complete, first, new(0, side[1])), Is.EqualTo("That game is complete, so it can't be changed."));
        complete = ScheduleEditing.Replace(session, 0, session.Rounds[0] with { Games = [session.Rounds[0].Games[0] with { Complete = true }, session.Rounds[0].Games[1]] });
        Assert.That(ScheduleEditing.Pin(complete, first), Is.SameAs(complete));
    }

    /// <summary>Checks sitting-out exchanges and persistence of all edit metadata.</summary>
    [Test]
    public void Swap_SittingOut_PreservesMembershipAndRoundTripsMarkers()
    {
        Session session = Create(5, 1) with { Editing = true };
        Guid first = session.Rounds[0].Games[0].Left[0];
        Guid second = session.Rounds[0].SittingOut[0];
        Session swapped = ScheduleEditing.Swap(session, new(0, first), new(0, second));
        Assert.That(swapped.Rounds[0].Games[0].Left[0], Is.EqualTo(second));
        Assert.That(swapped.Rounds[0].SittingOut[0], Is.EqualTo(first));
        Assert.That(swapped.Rounds[0].Moved, Is.EquivalentTo([first, second]));
        Assert.That(swapped.Rounds.Skip(1), Is.EqualTo(session.Rounds.Skip(1)));
        swapped = ScheduleEditing.Pin(swapped, new(0, second));
        Assert.That(SessionJson.Serialize(SessionJson.Deserialize(SessionJson.Serialize(swapped)) ?? throw new AssertionException("Missing session")), Is.EqualTo(SessionJson.Serialize(swapped)));
        Session finished = ScheduleEditing.Finish(swapped);
        Assert.That(finished.Editing, Is.False);
        Assert.That(finished.Rounds[0].Moved, Is.Empty);
        Assert.That(finished.Rounds[0].Pinned, Is.EqualTo([second]));
    }

    /// <summary>Checks repair keeps the user's changed round and fixes the other occurrence.</summary>
    [TestCase(4, 1)]
    [TestCase(24, 6)]
    [TestCase(8, 2)]
    [TestCase(6, 2)]
    [TestCase(7, 2)]
    [TestCase(5, 1)]
    public void Repair_ManualRepeat_PreservesEditedRoundAndHardRules(int count, int courts)
    {
        Session session = Create(count, courts);
        Session swapped = Repeat(session);
        Session repaired = ScheduleEngine.Repair(swapped, 0).Session;
        Assert.That(SessionJson.Serialize(ScheduleEngine.Repair(swapped, 0).Session), Is.EqualTo(SessionJson.Serialize(repaired)));
        Assert.That(Positions(repaired.Rounds[0]), Is.EqualTo(Positions(swapped.Rounds[0])), "Prefer changing the other round");
        if (count == 8)
        {
            Assert.That(repaired.Rounds.Select((round, index) => Positions(round).Zip(Positions(swapped.Rounds[index])).Count(pair => pair.First != pair.Second)).Sum(), Is.LessThanOrEqualTo(8), "Prefer nearby repairs");
        }

        Check(repaired);
    }

    /// <summary>Checks fixed positions, completed conflicts, chosen movement and accepted repeats.</summary>
    [Test]
    public void Repair_CompletedConflict_RespectsChoiceAndAcceptedPartnership()
    {
        Session swapped = Repeat(Create(8, 2));
        Guid[] repeat = swapped.Rounds[0].Games.SelectMany(game => new[] { game.Left, game.Right })
            .First(side => ScheduleEditing.Repeated(swapped, 0, side[0]));
        int other = Array.FindIndex(swapped.Rounds, 1, round => round.Games.Any(game => new[] { game.Left, game.Right }.Any(side => side.All(repeat.Contains))));
        Session completed = ScheduleEditing.Replace(swapped, other, swapped.Rounds[other] with { Complete = true });
        PartnerConflict conflict = ScheduleEditing.CompletedConflict(completed) ?? throw new AssertionException("Missing conflict");
        Session repaired = ScheduleEngine.Repair(completed, 0, [new(conflict.Round, conflict.First)]).Session;
        Assert.That(Positions(repaired.Rounds[other]), Is.EqualTo(Positions(completed.Rounds[other])));
        Assert.That(Array.IndexOf(Positions(repaired.Rounds[0]), conflict.First), Is.Not.EqualTo(Array.IndexOf(Positions(completed.Rounds[0]), conflict.First)));
        Check(repaired);
        List<PartnerConflict> choices = [];
        List<PlayerPosition> moves = [];
        while (ScheduleEditing.CompletedConflict(completed, [.. choices]) is { } pending)
        {
            choices.Add(pending);
            moves.Add(new(pending.Round, pending.First));
        }

        Assert.That(choices.Count, Is.GreaterThan(1));
        repaired = ScheduleEngine.Repair(completed, 0, [.. moves]).Session;
        foreach (PlayerPosition move in moves)
        {
            Assert.That(Array.IndexOf(Positions(repaired.Rounds[move.Round]), move.Player), Is.Not.EqualTo(Array.IndexOf(Positions(completed.Rounds[move.Round]), move.Player)));
        }

        Check(repaired);
        Session accepted = ScheduleEditing.Accept(completed, conflict);
        Assert.That(ScheduleEditing.Repeated(accepted, conflict.Round, conflict.First), Is.False);
        while (ScheduleEditing.CompletedConflict(accepted) is { } next)
        {
            accepted = ScheduleEditing.Accept(accepted, next);
        }

        Session kept = ScheduleEngine.Repair(accepted, 0).Session;
        Assert.That(Positions(kept.Rounds[conflict.Round]), Is.EqualTo(Positions(accepted.Rounds[conflict.Round])));
        Assert.That(SessionJson.Deserialize(SessionJson.Serialize(kept)), Is.Not.Null);
    }

    /// <summary>Checks pins survive repair and blocked searches do not silently unpin.</summary>
    [Test]
    public void Repair_PinnedPositions_PreservesPinsOrReportsFailure()
    {
        Session swapped = Repeat(Create(8, 2));
        Guid pin = swapped.Rounds[1].Games[0].Left[0];
        Session pinned = ScheduleEditing.Pin(swapped, new(1, pin));
        Session repaired = ScheduleEngine.Repair(pinned, 0).Session;
        Assert.That(Array.IndexOf(Positions(repaired.Rounds[1]), pin), Is.EqualTo(Array.IndexOf(Positions(pinned.Rounds[1]), pin)));
        Check(repaired);
        Session blocked = swapped with { Rounds = [.. swapped.Rounds.Select(round => round with { Pinned = Positions(round) })] };
        Assert.That(ScheduleEngine.Repair(blocked, 0), Is.EqualTo(new ScheduleRepairResult(blocked, false)));
        Assert.That(blocked.Rounds.All(round => round.Pinned.Length == 8), Is.True);
    }

    /// <summary>Checks unchanged schedules and reordering of singles before doubles.</summary>
    [Test]
    public void Repair_UnchangedReorderedSession_MakesNoChanges()
    {
        Session session = Create(6, 2);
        session = ScheduleOrder.Move(session, 0, 0, 1);
        Session repaired = ScheduleEngine.Repair(session, 4).Session;
        Assert.That(SessionJson.Serialize(repaired), Is.EqualTo(SessionJson.Serialize(session)));
    }

    /// <summary>Checks extra games retain their reordered court when a repair is needed.</summary>
    [Test]
    public void Repair_ReorderedThreePlayerGame_PreservesCourtOrderAndPins()
    {
        Session session = Repeat(Create(7, 2));
        Guid pin = session.Rounds[0].Games[1].Left[0];
        session = ScheduleEditing.Pin(session, new(0, pin));
        session = ScheduleOrder.Move(session, 0, 0, 1);
        Session repaired = ScheduleEngine.Repair(session, 0).Session;
        Assert.That(repaired.Rounds[0].Games[0].Left[0], Is.EqualTo(pin));
        Assert.That(repaired.Rounds[0].Games[0].Right.Length, Is.EqualTo(2));
        Check(repaired);
    }

    /// <summary>Checks older schema-one documents supply safe defaults for new fields.</summary>
    [Test]
    public void Deserialize_LegacySession_PreservesPlayersAndPositions()
    {
        Session session = Create(8, 2);
        string legacy = SessionJson.Serialize(session).Replace(",\"editing\":false", string.Empty, StringComparison.Ordinal)
            .Replace(",\"pinned\":[]", string.Empty, StringComparison.Ordinal)
            .Replace(",\"moved\":[]", string.Empty, StringComparison.Ordinal)
            .Replace(",\"edited\":[]", string.Empty, StringComparison.Ordinal)
            .Replace(",\"acceptedLeft\":false", string.Empty, StringComparison.Ordinal)
            .Replace(",\"acceptedRight\":false", string.Empty, StringComparison.Ordinal);
        Session restored = SessionJson.Deserialize(legacy) ?? throw new AssertionException("Missing session");
        Assert.That(SessionJson.Serialize(restored), Is.EqualTo(SessionJson.Serialize(session)));
        string invalid = SessionJson.Serialize(session).Replace("\"pinned\":[]", "\"pinned\":null", StringComparison.Ordinal);
        Assert.That(SessionJson.Deserialize(invalid)?.Rounds[0].Pinned, Is.Empty);
    }

    /// <summary>Checks a swap updates singles rank warnings immediately.</summary>
    [Test]
    public void Swap_SinglesRanks_RecalculatesRedGameWarning()
    {
        Session session = Create(6, 2);
        Guid one = session.Rounds[0].Games[1].Left[0];
        Guid three = session.Rounds[0].Games[0].Left[0];
        session = session with { Players = [.. session.Players.Select(player => player with { Rank = player.Id == one ? 1 : player.Id == three ? 3 : null })] };
        Session swapped = ScheduleEditing.Swap(session, new(0, three), new(0, session.Rounds[0].Games[1].Right[0]));
        Assert.That(swapped.Rounds[0].Games[1].RankConflict, Is.True);
    }

    internal static Session Create(int count, int courts)
    {
        Player[] players = ScheduleTests.Players(count);
        return new(1, ScheduleStep.Review, [.. Enumerable.Range(1, courts)], players, ScheduleEngine.Generate(players, courts, 4), false);
    }

    internal static Session Repeat(Session session)
    {
        Round round = session.Rounds[0];
        Guid first = round.Games[0].Left[0];
        Guid partner = round.Games[0].Left[1];
        Guid target = session.Rounds.Skip(1).SelectMany(item => item.Games).SelectMany(game => new[] { game.Left, game.Right })
            .First(side => side.Length == 2 && side.Contains(first) && !side.Contains(partner)).First(id => id != first);
        return ScheduleEditing.Swap(session, new(0, partner), new(0, target));
    }

    private static Guid[] Positions(Round round)
    {
        return [.. round.Games.SelectMany(game => game.Left.Concat(game.Right)), .. round.SittingOut];
    }

    private static void Check(Session session)
    {
        HashSet<(Guid, Guid)> partners = [];
        Dictionary<(Guid, Guid), int> opponents = [];
        foreach (Round round in session.Rounds)
        {
            Assert.That(Positions(round), Is.EquivalentTo(session.Players.Select(player => player.Id)));
            foreach (Game game in round.Games)
            {
                foreach (Guid[] side in new[] { game.Left, game.Right }.Where(side => side.Length == 2))
                {
                    Assert.That(partners.Add(Pair(side[0], side[1])), Is.True);
                }

                foreach (Guid left in game.Left)
                {
                    foreach (Guid right in game.Right)
                    {
                        (Guid First, Guid Second) pair = Pair(left, right);
                        opponents[pair] = opponents.GetValueOrDefault(pair) + 1;
                    }
                }
            }
        }

        Assert.That(opponents.Values.Max(), Is.LessThanOrEqualTo(3));
        for (int size = 1; size <= 3; size++)
        {
            int category = size;
            int[] shares = [.. session.Players.Select(player => session.Rounds.Count(round => category == 1
                ? round.SittingOut.Contains(player.Id)
                : round.Games.Any(game => game.Left.Length + game.Right.Length == category && game.Left.Concat(game.Right).Contains(player.Id))))];
            Assert.That(shares.Max() - shares.Min(), Is.LessThanOrEqualTo(1));
        }
    }

    private static (Guid First, Guid Second) Pair(Guid first, Guid second)
    {
        return first.CompareTo(second) < 0 ? (first, second) : (second, first);
    }
}
