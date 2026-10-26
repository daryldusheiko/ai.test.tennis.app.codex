using SocialTennis.Players;

namespace SocialTennis.Scheduling;

/// <summary>Readable courtside names, disambiguated across the entire session.</summary>
public static class ScheduleDisplay
{
    /// <summary>Uses full names for players whose abbreviated names would collide.</summary>
    public static string Name(Player[] players, Guid id)
    {
        Player player = players.First(item => item.Id == id);
        string shortName = Abbreviate(player);
        return players.Any(other => other.Id != id && string.Equals(Abbreviate(other), shortName, StringComparison.OrdinalIgnoreCase))
            ? player.FullName : shortName;
    }

    private static string Abbreviate(Player player)
    {
        return string.IsNullOrWhiteSpace(player.LastName)
            ? player.FirstName : $"{player.FirstName} {System.Globalization.StringInfo.GetNextTextElement(player.LastName)}.";
    }
}
