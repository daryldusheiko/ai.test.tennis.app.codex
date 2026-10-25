namespace SocialTennis.Scheduling;

/// <summary>Reorders only movable games and rounds.</summary>
public static class ScheduleOrder
{
    /// <summary>Moves a round (game = -1) or a game within its round.</summary>
    public static Session Move(Session session, int round, int game, int target)
    {
        if (round < 0 || round >= session.Rounds.Length || session.Rounds[round].Complete)
        {
            return session;
        }

        if (game < 0)
        {
            if (target < 0 || target >= session.Rounds.Length ||
                session.Rounds.Skip(Math.Min(round, target)).Take(Math.Abs(round - target) + 1)
                    .Any(item => item.Complete || item.Games.Any(match => match.Complete)))
            {
                return session;
            }

            return session with { Rounds = MoveItem(session.Rounds, round, target) };
        }

        Game[] games = session.Rounds[round].Games;
        if (game >= games.Length || target < 0 || target >= games.Length ||
            games.Skip(Math.Min(game, target)).Take(Math.Abs(game - target) + 1).Any(item => item.Complete))
        {
            return session;
        }

        Round[] rounds = [.. session.Rounds];
        rounds[round] = rounds[round] with { Games = MoveItem(games, game, target) };
        return session with { Rounds = rounds };
    }

    private static T[] MoveItem<T>(T[] source, int from, int to)
    {
        List<T> items = [.. source];
        T moved = items[from];
        items.RemoveAt(from);
        items.Insert(to, moved);
        return [.. items];
    }
}
