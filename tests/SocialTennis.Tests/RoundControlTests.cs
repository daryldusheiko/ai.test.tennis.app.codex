using System.Text.Json;
using NUnit.Framework;
using SocialTennis.Players;
using SocialTennis.Scheduling;

namespace SocialTennis.Tests;

/// <summary>Checks round lifecycle, persistence and courtside naming.</summary>
[TestFixture]
public sealed class RoundControlTests
{
    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Start_OutOfOrder_OnlyOneRoundCanPlay()
    {
        Session session = ScheduleEditingTests.Create(8, 2) with { InPlay = true };
        Session started = RoundControl.Start(session, 4);
        Assert.That(started.Rounds[4].InPlay, Is.True);
        Assert.That(started.Rounds.Count(round => round.InPlay), Is.EqualTo(1));
        Assert.That(RoundControl.Start(started, 0), Is.SameAs(started));
        Assert.That(RoundControl.Start(started, 4), Is.SameAs(started));
        Assert.That(session.Rounds.Any(round => round.InPlay), Is.False);
        Assert.That(started.Rounds[4].Games, Is.SameAs(session.Rounds[4].Games));
    }

    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Cancel_ActiveRound_ReleasesSlotWithoutChangingGames()
    {
        Session started = RoundControl.Start(ScheduleEditingTests.Create(8, 2) with { InPlay = true }, 3);
        Session cancelled = RoundControl.Cancel(started, 3);
        Assert.That(cancelled.Rounds.Any(round => round.InPlay), Is.False);
        Assert.That(cancelled.Rounds[3].Complete, Is.False);
        Assert.That(cancelled.Rounds[3].Games, Is.SameAs(started.Rounds[3].Games));
        Assert.That(RoundControl.Start(cancelled, 1).Rounds[1].InPlay, Is.True);
    }

    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Finish_ActiveRound_CompletesEveryGameAndLocksRound()
    {
        Session started = RoundControl.Start(ScheduleEditingTests.Create(8, 2) with { InPlay = true }, 2);
        Session finished = RoundControl.Finish(started, 2);
        Assert.That(finished.Rounds[2].Complete, Is.True);
        Assert.That(finished.Rounds[2].InPlay, Is.False);
        Assert.That(finished.Rounds[2].Games.All(game => game.Complete), Is.True);
        Assert.That(finished.Rounds.Where((_, index) => index != 2), Is.EqualTo(started.Rounds.Where((_, index) => index != 2)));
        Assert.That(RoundControl.Start(finished, 2), Is.SameAs(finished));
        Assert.That(RoundControl.Cancel(finished, 2), Is.SameAs(finished));
        Assert.That(RoundControl.Finish(finished, 2), Is.SameAs(finished));
        Assert.That(RoundControl.Start(finished, 0).Rounds[0].InPlay, Is.True);
    }

    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Transitions_InvalidOrUnstartedRound_LeaveSessionUnchanged()
    {
        Session session = ScheduleEditingTests.Create(8, 2) with { InPlay = true };
        foreach (int index in new[] { -1, 99, 0 })
        {
            Assert.That(RoundControl.Cancel(session, index), Is.SameAs(session));
            Assert.That(RoundControl.Finish(session, index), Is.SameAs(session));
        }

        Assert.That(RoundControl.Start(session, -1), Is.SameAs(session));
        Assert.That(RoundControl.Start(session, 99), Is.SameAs(session));
        Session setup = session with { InPlay = false };
        Assert.That(RoundControl.Start(setup, 0), Is.SameAs(setup));
    }

    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Json_ActiveCompletedAndLegacyRounds_RetainLifecycle()
    {
        Session session = ScheduleEditingTests.Create(8, 2) with { InPlay = true };
        session = RoundControl.Start(RoundControl.Finish(RoundControl.Start(session, 2), 2), 4);
        Session loaded = SessionJson.Deserialize(SessionJson.Serialize(session)) ?? throw new AssertionException("Missing session");
        Assert.That(loaded.Rounds[2].Complete, Is.True);
        Assert.That(loaded.Rounds[2].Games.All(game => game.Complete), Is.True);
        Assert.That(loaded.Rounds[4].InPlay, Is.True);
        string legacy = SessionJson.Serialize(ScheduleEditingTests.Create(8, 2)).Replace("\"inPlay\":false,", string.Empty, StringComparison.Ordinal);
        Assert.That(SessionJson.Deserialize(legacy)?.Rounds.All(round => !round.InPlay), Is.True);
        Session invalid = session with { Rounds = [.. session.Rounds.Select(round => round with { InPlay = true })] };
        Assert.Throws<JsonException>(() => SessionJson.Deserialize(SessionJson.Serialize(invalid)));
        invalid = session with { Rounds = [.. session.Rounds.Select(round => round with { InPlay = round.Complete })] };
        Assert.Throws<JsonException>(() => SessionJson.Deserialize(SessionJson.Serialize(invalid)));
    }

    /// <summary>Verifies the lifecycle or display rule independently of browser state.</summary>
    [Test]
    public void Name_InitialsAndCollisions_UsesReadableUnambiguousNames()
    {
        Player[] players =
        [
            new(Guid.NewGuid(), "Daryl", "Davis", null, string.Empty),
            new(Guid.NewGuid(), "Ann", "Smith", null, string.Empty),
            new(Guid.NewGuid(), "ann", "Stone", null, string.Empty),
            new(Guid.NewGuid(), "Jo", string.Empty, null, string.Empty),
            new(Guid.NewGuid(), "Mary Ann", "De Silva", null, string.Empty),
        ];
        Assert.That(players.Select(player => ScheduleDisplay.Name(players, player.Id)), Is.EqualTo((string[])["Daryl D.", "Ann Smith", "ann Stone", "Jo", "Mary Ann D."]));
    }
}
