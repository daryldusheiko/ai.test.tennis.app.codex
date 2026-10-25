using System.Text.Json;

namespace SocialTennis.Scheduling;

/// <summary>Trimming-safe versioned session serialization.</summary>
public static class SessionJson
{
    /// <summary>Serializes the complete session.</summary>
    public static string Serialize(Session session)
    {
        return JsonSerializer.Serialize(session, SessionJsonContext.Default.Session);
    }

    /// <summary>Reads and validates saved sessions without overwriting damaged data.</summary>
    public static Session? Deserialize(string? json)
    {
        if (json is null)
        {
            return null;
        }

        Session session = JsonSerializer.Deserialize(json, SessionJsonContext.Default.Session)
            ?? throw new JsonException("Schedule storage contained no data.");
        if (session.Version != 1 || !Enum.IsDefined(session.Step) || session.Courts is null ||
            session.Players is null || session.Rounds is null || session.Courts.Any(court => court < 1) ||
            session.Courts.Distinct().Count() != session.Courts.Length ||
            session.Players.Any(player => player is null || player.FirstName is null || player.LastName is null) ||
            session.Players.Select(player => player.Id).Distinct().Count() != session.Players.Length)
        {
            throw new JsonException("Schedule storage contains an incomplete session.");
        }

        HashSet<Guid> players = [.. session.Players.Select(player => player.Id)];
        foreach (Round round in session.Rounds)
        {
            if (round is null || round.Games is null || round.SittingOut is null ||
                round.Games.Any(game => game is null || game.Left is null || game.Right is null ||
                    game.Left.Length is < 1 or > 2 || game.Right.Length is < 1 or > 2))
            {
                throw new JsonException("Schedule storage contains an incomplete round.");
            }

            Guid[] participants = [.. round.Games.SelectMany(game => game.Left.Concat(game.Right)), .. round.SittingOut];
            if (participants.Length != players.Count || !players.SetEquals(participants) || round.Games.Length > session.Courts.Length)
            {
                throw new JsonException("Schedule storage contains invalid player positions.");
            }
        }

        if ((session.Step != ScheduleStep.Courts && session.Courts.Length == 0) ||
            (session.Step == ScheduleStep.Review && (session.Players.Length < 4 || session.Rounds.Length == 0)) ||
            (session.InPlay && session.Step != ScheduleStep.Review))
        {
            throw new JsonException("Schedule storage contains an invalid step.");
        }

        return session with { Courts = [.. session.Courts.Order()] };
    }
}
