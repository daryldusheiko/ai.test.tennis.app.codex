namespace SocialTennis.Scheduling;

/// <summary>Pure editing rules; positions and identities stay within their original round.</summary>
public static class ScheduleEditing
{
    /// <summary>Gets the saved full name.</summary>
    public static string Name(Session session, Guid id)
    {
        return session.Players.First(player => player.Id == id).FullName;
    }

    /// <summary>Finds a game, or null when sitting out.</summary>
    public static Game? GameAt(Session session, PlayerPosition position)
    {
        return session.Rounds[position.Round].Games
        .FirstOrDefault(game => game.Left.Contains(position.Player) || game.Right.Contains(position.Player));
    }

    /// <summary>Checks completed state.</summary>
    public static bool Locked(Session session, PlayerPosition position)
    {
        return session.Rounds[position.Round].Complete || GameAt(session, position)?.Complete == true;
    }

    /// <summary>Toggles a game position pin unless complete.</summary>
    public static Session Pin(Session session, PlayerPosition position)
    {
        Round round = session.Rounds[position.Round];
        if (Locked(session, position) || GameAt(session, position) is null)
        {
            return session;
        }

        return Replace(session, position.Round, round with
        {
            Pinned = round.Pinned.Contains(position.Player) ? [.. round.Pinned.Except([position.Player])] : [.. round.Pinned, position.Player],
        });
    }

    /// <summary>Explains why a swap cannot be made.</summary>
    public static string? SwapError(Session session, PlayerPosition first, PlayerPosition second)
    {
        if (first.Round != second.Round)
        {
            return $"Pick a player in round {first.Round + 1}.";
        }

        foreach (PlayerPosition position in new[] { first, second })
        {
            if (Locked(session, position))
            {
                return "That game is complete, so it can't be changed.";
            }

            if (session.Rounds[position.Round].Pinned.Contains(position.Player))
            {
                return $"{Name(session, position.Player)} is pinned and can't be swapped.";
            }
        }

        Game? game = GameAt(session, first);
        if (first.Player == second.Player || (game is not null && new[] { game.Left, game.Right }.Any(side => side.Contains(first.Player) && side.Contains(second.Player))))
        {
            return $"{Name(session, first.Player)} and {Name(session, second.Player)} are already on the same side.";
        }

        return null;
    }

    /// <summary>Exchanges two allowed positions and records the manual edit.</summary>
    public static Session Swap(Session session, PlayerPosition first, PlayerPosition second)
    {
        if (SwapError(session, first, second) is not null)
        {
            return session;
        }

        Guid Map(Guid id) => id == first.Player ? second.Player : id == second.Player ? first.Player : id;
        Round round = session.Rounds[first.Round];
        return Replace(session, first.Round, round with
        {
            Games = [.. round.Games.Select(game => game with
            {
                Left = [.. game.Left.Select(Map)], Right = [.. game.Right.Select(Map)],
                AcceptedLeft = game.AcceptedLeft && !game.Left.Any(id => id == first.Player || id == second.Player),
                AcceptedRight = game.AcceptedRight && !game.Right.Any(id => id == first.Player || id == second.Player),
            })],
            SittingOut = [.. round.SittingOut.Select(Map)],
            Moved = [.. round.Moved.Union([first.Player, second.Player])],
            Edited = [.. round.Edited.Union([first.Player, second.Player])],
        });
    }

    /// <summary>Checks unaccepted repeat partnerships.</summary>
    public static bool Repeated(Session session, int round, Guid id)
    {
        Game? game = GameAt(session, new(round, id));
        Guid[]? side = game?.Left.Contains(id) == true ? game.Left : game?.Right;
        if (game is null || side is not { Length: 2 } || (game.Left == side ? game.AcceptedLeft : game.AcceptedRight))
        {
            return false;
        }

        return session.Rounds.Where((_, index) => index != round).SelectMany(item => item.Games)
            .Any(other => (other.Left.Length == 2 && !other.AcceptedLeft && other.Left.All(side.Contains)) ||
                (other.Right.Length == 2 && !other.AcceptedRight && other.Right.All(side.Contains)));
    }

    /// <summary>Finds the next repeat requiring a user choice.</summary>
    public static PartnerConflict? CompletedConflict(Session session, PartnerConflict[]? decided = null)
    {
        for (int r = 0; r < session.Rounds.Length; r++)
        {
            Round round = session.Rounds[r];
            for (int g = 0; g < round.Games.Length; g++)
            {
                Game game = round.Games[g];
                if (round.Complete || game.Complete)
                {
                    continue;
                }

                foreach (bool left in new[] { true, false })
                {
                    Guid[] side = left ? game.Left : game.Right;
                    if (side.Length != 2 || (left ? game.AcceptedLeft : game.AcceptedRight) ||
                        decided?.Any(choice => choice.Round == r && choice.Game == g && choice.Left == left) == true)
                    {
                        continue;
                    }

                    for (int previous = 0; previous < session.Rounds.Length; previous++)
                    {
                        if (previous != r && session.Rounds[previous].Games.Any(other =>
                            (session.Rounds[previous].Complete || other.Complete) && new[] { other.Left, other.Right }.Any(team => team.Length == 2 && team.All(side.Contains))))
                        {
                            return new(previous, r, g, left, side[0], side[1]);
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>Accepts the pending repeat without moving either partner.</summary>
    public static Session Accept(Session session, PartnerConflict conflict)
    {
        Round round = session.Rounds[conflict.Round];
        Game[] games = [.. round.Games];
        Game game = games[conflict.Game];
        games[conflict.Game] = conflict.Left ? game with { AcceptedLeft = true } : game with { AcceptedRight = true };
        return Replace(session, conflict.Round, round with { Games = games });
    }

    /// <summary>Leaves edit mode and clears blue markers.</summary>
    public static Session Finish(Session session)
    {
        return session with
        {
            Editing = false,
            Rounds = [.. session.Rounds.Select(round => round with { Moved = [] })],
        };
    }

    /// <summary>Returns a session with one replaced round.</summary>
    public static Session Replace(Session session, int index, Round round)
    {
        Round[] rounds = [.. session.Rounds];
        rounds[index] = round with
        {
            Games = [.. round.Games.Select(game => game with
            {
                RankConflict = game.Left.Length == 1 && game.Right.Length == 1 &&
                    ((session.Players.First(player => player.Id == game.Left[0]).Rank == 1 && session.Players.First(player => player.Id == game.Right[0]).Rank == 3) ||
                    (session.Players.First(player => player.Id == game.Left[0]).Rank == 3 && session.Players.First(player => player.Id == game.Right[0]).Rank == 1)),
            })],
        };
        return session with { Rounds = rounds };
    }
}
