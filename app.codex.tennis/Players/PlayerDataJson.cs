using System.Text.Json;

namespace SocialTennis.Players;

/// <summary>Converts versioned player documents to and from JSON.</summary>
public static class PlayerDataJson
{
    /// <summary>Encodes a document using trimming-safe metadata.</summary>
    public static string Serialize(PlayerData data)
    {
        return JsonSerializer.Serialize(data, PlayerJsonContext.Default.PlayerData);
    }

    /// <summary>Reads saved data and normalizes legacy ranks.</summary>
    public static PlayerData Deserialize(string? json)
    {
        if (json is null)
        {
            return PlayerData.Empty;
        }

        PlayerData data = JsonSerializer.Deserialize(json, PlayerJsonContext.Default.PlayerData)
            ?? throw new JsonException("Player storage contained no data.");
        if (data.Players is null || data.Players.Any(p => p is null || p.FirstName is null || p.LastName is null || p.Comment is null))
        {
            throw new JsonException("Player storage contains an incomplete player.");
        }

        return data with
        {
            Players = [.. data.Players.Select(p => p with { Rank = p.Rank > 3 ? 3 : p.Rank })],
            Sort = Enum.IsDefined(data.Sort) ? data.Sort : PlayerSort.FirstNameAscending,
        };
    }
}
