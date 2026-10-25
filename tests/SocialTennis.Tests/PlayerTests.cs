using System.Text.Json;
using Microsoft.JSInterop;
using NUnit.Framework;
using SocialTennis.Players;

namespace SocialTennis.Tests;

/// <summary>Checks player behaviour and persistence.</summary>
[TestFixture]
public sealed class PlayerTests
{
    /// <summary>Checks player behaviour and persistence.</summary>
    [TestCase("", "", null, "", "First name is required.")]
    [TestCase("   ", "", null, "", "First name is required.")]
    [TestCase("Ann", "", 0, "", "Rank must be between 1 and 3.")]
    [TestCase("Ann", "", 4, "", "Rank must be between 1 and 3.")]
    public void Validate_InvalidValue_ReturnsExactMessage(string firstName, string lastName, int? rank, string comment, string expected)
    {
        Assert.That(PlayerRules.Validate(firstName, lastName, rank, comment), Does.Contain(expected));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [Test]
    public void Validate_LengthBoundaries_AcceptsLimitsAndRejectsExcess()
    {
        Assert.That(PlayerRules.Validate(new('a', 50), new('b', 50), null, new('c', 500)), Is.Empty);
        Assert.That(PlayerRules.Validate(new('a', 51), new('b', 51), null, new('c', 501)), Is.EqualTo(
        [
            "First name must be 50 characters or fewer.",
            "Last name must be 50 characters or fewer.",
            "Comment must be 500 characters or fewer.",
        ]));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [TestCase(null)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Validate_AllowedRanks_Accepts(int? rank)
    {
        Assert.That(PlayerRules.Validate("Ann", string.Empty, rank, string.Empty), Is.Empty);
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [TestCase(" ANN sm ", 1)]
    [TestCase("smith ann", 1)]
    [TestCase("ann missing", 0)]
    [TestCase("", 3)]
    public void Find_MultipleWords_MatchesEveryWordIgnoringCase(string search, int count)
    {
        Player[] players = [Create("Ann", "Smith"), Create("Ann", "Jones"), Create("Bob", "Smith")];
        Assert.That(PlayerRules.Find(players, search, PlayerSort.FirstNameAscending).Count(), Is.EqualTo(count));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [TestCase(PlayerSort.FirstNameAscending, "Ann Alpha,Ann Zulu,Bob Alpha")]
    [TestCase(PlayerSort.FirstNameDescending, "Bob Alpha,Ann Zulu,Ann Alpha")]
    [TestCase(PlayerSort.LastNameAscending, "Ann Alpha,Bob Alpha,Ann Zulu")]
    [TestCase(PlayerSort.LastNameDescending, "Ann Zulu,Bob Alpha,Ann Alpha")]
    public void Find_SortOptions_OrdersWithOtherNameTieBreaker(PlayerSort sort, string expected)
    {
        Player[] players = [Create("Bob", "Alpha"), Create("Ann", "Zulu"), Create("Ann", "Alpha")];
        Assert.That(string.Join(',', PlayerRules.Find(players, string.Empty, sort).Select(p => p.FullName)), Is.EqualTo(expected));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [Test]
    public async Task Storage_SaveAndReopen_RoundTripsAllFieldsAndSortAsync()
    {
        var javaScript = new StorageJavaScript();
        var storage = new BrowserPlayerStorage(javaScript);
        var data = new PlayerData(1, [Create("Mary Ann", "De Silva") with { Rank = 2, Comment = "Notes\nwith \"quotes\" 🎾" }], PlayerSort.LastNameDescending);
        await storage.SaveAsync(data);
        PlayerData loaded = await new BrowserPlayerStorage(javaScript).LoadAsync();
        Assert.That(loaded.Players, Is.EqualTo(data.Players));
        Assert.That(loaded.Sort, Is.EqualTo(data.Sort));
        Assert.That(loaded.Version, Is.EqualTo(1));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [Test]
    public void Deserialize_OlderHighRanksAndUnknownFields_PreservesPlayersAndClampsRank()
    {
        PlayerData data = PlayerDataJson.Deserialize("""
            {"version":1,"players":[{"id":"00000000-0000-0000-0000-000000000001","firstName":"Ann","lastName":"Smith","rank":8,"comment":"Keep me","future":true}],"sort":99,"future":true}
            """);
        Assert.That(data.Players.Single().Rank, Is.EqualTo(3));
        Assert.That(data.Players.Single().Comment, Is.EqualTo("Keep me"));
        Assert.That(data.Sort, Is.EqualTo(PlayerSort.FirstNameAscending));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [Test]
    public void Deserialize_EmptyOrDamagedStorage_InitializesOrRejectsWithoutOverwriting()
    {
        Assert.That(PlayerDataJson.Deserialize(null).Players, Is.Empty);
        Assert.Throws<JsonException>(() => PlayerDataJson.Deserialize("bad json"));
        Assert.Throws<JsonException>(() => PlayerDataJson.Deserialize("{\"players\":null}"));
    }

    /// <summary>Checks player behaviour and persistence.</summary>
    [Test]
    public void Storage_Unavailable_PropagatesFailure()
    {
        var storage = new BrowserPlayerStorage(new StorageJavaScript { Fail = true });
        Assert.ThrowsAsync<JSException>(() => storage.LoadAsync());
        Assert.ThrowsAsync<JSException>(() => storage.SaveAsync(PlayerData.Empty));
    }

    private static Player Create(string firstName, string lastName)
    {
        return new(Guid.Empty, firstName, lastName, null, string.Empty);
    }

    private sealed class StorageJavaScript : IJSRuntime
    {
        private string? _json;

        /// <summary>Gets a value indicating whether storage should fail.</summary>
        public bool Fail { get; init; }

        /// <summary>Checks player behaviour and persistence.</summary>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        /// <summary>Checks player behaviour and persistence.</summary>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Fail)
            {
                throw new JSException("Storage unavailable.");
            }

            if (identifier == "localStorage.setItem")
            {
                _json = (string?)args?[1];

                // IJSRuntime permits null for void results and nullable storage reads.
                return ValueTask.FromResult(default(TValue)!);
            }

            return ValueTask.FromResult((TValue)(object?)_json!);
        }
    }
}
