namespace SocialTennis.Scheduling;

/// <summary>Pure, atomic transitions for the single active round.</summary>
public static class RoundControl
{
    /// <summary>Starts any unfinished round when no other round is active.</summary>
    public static Session Start(Session session, int round)
    {
        if (!CanChange(session, round) || session.Rounds.Any(item => item.InPlay))
        {
            return session;
        }

        return Replace(session, round, session.Rounds[round] with { InPlay = true });
    }

    /// <summary>Returns an active round to not started, retaining its games.</summary>
    public static Session Cancel(Session session, int round)
    {
        return CanChange(session, round) && session.Rounds[round].InPlay
            ? Replace(session, round, session.Rounds[round] with { InPlay = false }) : session;
    }

    /// <summary>Completes an active round and every game in it.</summary>
    public static Session Finish(Session session, int round)
    {
        return CanChange(session, round) && session.Rounds[round].InPlay
            ? Replace(session, round, session.Rounds[round] with
            {
                InPlay = false,
                Complete = true,
                Games = [.. session.Rounds[round].Games.Select(game => game with { Complete = true })],
            }) : session;
    }

    private static bool CanChange(Session session, int round)
    {
        return session.InPlay && round >= 0 && round < session.Rounds.Length && !session.Rounds[round].Complete;
    }

    private static Session Replace(Session session, int index, Round round)
    {
        Round[] rounds = [.. session.Rounds];
        rounds[index] = round;
        return session with { Rounds = rounds };
    }
}
