namespace SocialTennis.Players;

/// <summary>Validates, searches and sorts players independently of the UI.</summary>
public static class PlayerRules
{
    /// <summary>Validates editable player fields.</summary>
    public static IReadOnlyList<string> Validate(string firstName, string lastName, int? rank, string comment)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add("First name is required.");
        }

        if (firstName.Length > 50)
        {
            errors.Add("First name must be 50 characters or fewer.");
        }

        if (lastName.Length > 50)
        {
            errors.Add("Last name must be 50 characters or fewer.");
        }

        if (rank is < 1 or > 3)
        {
            errors.Add("Rank must be between 1 and 3.");
        }

        if (comment.Length > 500)
        {
            errors.Add("Comment must be 500 characters or fewer.");
        }

        return errors;
    }

    /// <summary>Filters names by every word and orders the results.</summary>
    public static IEnumerable<Player> Find(IEnumerable<Player> players, string search, PlayerSort sort)
    {
        string[] words = search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<Player> matches = players.Where(player => words.All(word =>
            player.FirstName.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            player.LastName.Contains(word, StringComparison.OrdinalIgnoreCase)));
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        return sort switch
        {
            PlayerSort.FirstNameDescending => matches.OrderByDescending(p => p.FirstName, comparer).ThenByDescending(p => p.LastName, comparer),
            PlayerSort.LastNameAscending => matches.OrderBy(p => p.LastName, comparer).ThenBy(p => p.FirstName, comparer),
            PlayerSort.LastNameDescending => matches.OrderByDescending(p => p.LastName, comparer).ThenByDescending(p => p.FirstName, comparer),
            _ => matches.OrderBy(p => p.FirstName, comparer).ThenBy(p => p.LastName, comparer),
        };
    }
}
