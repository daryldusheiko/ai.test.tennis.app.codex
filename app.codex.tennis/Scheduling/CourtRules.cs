using System.Globalization;

namespace SocialTennis.Scheduling;

/// <summary>Validates comma-separated court numbers.</summary>
public static class CourtRules
{
    /// <summary>Parses and sorts courts, reporting the first error.</summary>
    public static CourtSelection Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new([], "Enter at least one court number, for example 4,5,6.");
        }

        List<int> courts = [];
        foreach (string entry in text.Split(',').Select(value => value.Trim()))
        {
            if (!int.TryParse(entry, NumberStyles.Integer, CultureInfo.InvariantCulture, out int court))
            {
                return new([], $"“{entry}” is not a court number.");
            }

            if (court < 1)
            {
                return new([], "Court numbers must be 1 or more.");
            }

            if (courts.Contains(court))
            {
                return new([], $"Court {court} is listed more than once.");
            }

            courts.Add(court);
        }

        return new([.. courts.Order()], null);
    }
}
