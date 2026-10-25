using System.Text.Json;
using Microsoft.JSInterop;
using NUnit.Framework;
using SocialTennis.Players;
using SocialTennis.Scheduling;

namespace SocialTennis.Tests;

/// <summary>Checks persistence and ordering independently of browser rendering.</summary>
[TestFixture]
public sealed class SessionTests
{
    /// <summary>Checks each saved step survives reopening.</summary>
    [Test]
    public async Task Storage_EveryStep_RoundTripsAsync()
    {
        var javaScript = new StorageJavaScript();
        var storage = new BrowserScheduleStorage(javaScript);
        Assert.That(await storage.LoadAsync(), Is.Null);
        Session session = Session.Empty;
        foreach (ScheduleStep step in Enum.GetValues<ScheduleStep>())
        {
            session = session with { Courts = [2, 4], Players = ScheduleTests.Players(8), Step = step };
            if (step == ScheduleStep.Review)
            {
                session = session with { Rounds = ScheduleEngine.Generate(session.Players, 2, 4), InPlay = true };
            }

            await storage.SaveAsync(session);
            Assert.That(SessionJson.Serialize(await new BrowserScheduleStorage(javaScript).LoadAsync() ?? throw new AssertionException("Missing session")), Is.EqualTo(SessionJson.Serialize(session)));
        }

        javaScript.Fail = true;
        Assert.ThrowsAsync<JSException>(() => storage.LoadAsync());
        Assert.ThrowsAsync<JSException>(() => storage.SaveAsync(session));
    }

    /// <summary>Checks corrupt data is rejected without writes.</summary>
    [TestCase("broken")]
    [TestCase("null")]
    [TestCase("{\"version\":2}")]
    [TestCase("{\"version\":1,\"step\":99,\"courts\":[],\"players\":[],\"rounds\":[]}")]
    [TestCase("{\"version\":1,\"step\":0,\"courts\":[0],\"players\":[],\"rounds\":[]}")]
    public void Deserialize_DamagedSession_Rejects(string json)
    {
        Assert.Throws<JsonException>(() => SessionJson.Deserialize(json));
    }

    /// <summary>Checks movement keeps all identities and blocks completed positions.</summary>
    [Test]
    public void Move_RoundsAndGames_PreservesMembershipAndCompletion()
    {
        Player[] players = ScheduleTests.Players(8);
        var session = new Session(1, ScheduleStep.Review, [4, 5], players, ScheduleEngine.Generate(players, 2, 5), false);
        Session moved = ScheduleOrder.Move(session, 0, -1, 2);
        Assert.That(moved.Rounds[2], Is.SameAs(session.Rounds[0]));
        Assert.That(moved.Rounds[0], Is.SameAs(session.Rounds[1]));
        moved = ScheduleOrder.Move(session, 0, 0, 1);
        Assert.That(moved.Rounds[0].Games[1], Is.SameAs(session.Rounds[0].Games[0]));
        Assert.That(moved.Rounds.Skip(1), Is.EqualTo(session.Rounds.Skip(1)));
        Round[] rounds = [.. session.Rounds];
        rounds[1] = rounds[1] with { Complete = true };
        Session completed = session with { Rounds = rounds };
        Assert.That(ScheduleOrder.Move(completed, 0, -1, 2), Is.SameAs(completed));
        Assert.That(ScheduleOrder.Move(completed, 1, 0, 1), Is.SameAs(completed));
        rounds[0] = rounds[0] with { Games = [rounds[0].Games[0] with { Complete = true }, rounds[0].Games[1]] };
        Assert.That(ScheduleOrder.Move(completed, 0, 1, 0), Is.SameAs(completed));
        Assert.That(ScheduleOrder.Move(completed, -1, -1, 0), Is.SameAs(completed));
        Assert.That(ScheduleOrder.Move(completed, 0, 0, 99), Is.SameAs(completed));
    }

    private sealed class StorageJavaScript : IJSRuntime
    {
        private string? _json;

        public bool Fail { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Fail)
            {
                throw new JSException("Storage unavailable.");
            }

            if (identifier == "localStorage.setItem")
            {
                _json = (string?)args?[1];
            }

            // JS interop permits null for nullable reads and void results.
            return ValueTask.FromResult(identifier == "localStorage.setItem" ? default! : (TValue)(object?)_json!);
        }
    }
}
