using SocialTennis.Players;

namespace SocialTennis.Scheduling;

/// <summary>Seeded local search over player permutations; every move preserves round membership.</summary>
public static class ScheduleEngine
{
    /// <summary>Generates reproducible rounds with no repeated partners.</summary>
    public static Round[] Generate(Player[] players, int courtCount, int seed)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (players.Length < 4 || courtCount < 1 || players.Select(player => player.Id).Distinct().Count() != players.Length)
        {
            throw new ArgumentException("A schedule needs at least four distinct players and one court.");
        }

        int rounds = players.Length switch { 4 => 3, 5 => 5, _ => 6 };
        var search = new Search(players, courtCount, rounds, seed);
        return search.Run();
    }

    /// <summary>Repairs a saved schedule without moving fixed positions.</summary>
    public static ScheduleRepairResult Repair(Session session, int seed, PlayerPosition[]? mustMove = null)
    {
        if (mustMove is null || mustMove.Length == 0)
        {
            try
            {
                var preferred = new Search(session.Players, session.Courts.Length, session.Rounds.Length, seed, session, null, true);
                return new(session with { Rounds = preferred.Run() }, true);
            }
            catch (InvalidOperationException)
            {
                // A manual round is a preference; pins and completion remain hard constraints below.
            }
        }

        try
        {
            var search = new Search(session.Players, session.Courts.Length, session.Rounds.Length, seed, session, mustMove);
            return new(session with { Rounds = search.Run() }, true);
        }
        catch (InvalidOperationException)
        {
            return new(session, false);
        }
    }

    private sealed class Search
    {
        private const int HardPenalty = 100000;
        private const int RankPenalty = 1000;
        private readonly Player[] _players;
        private readonly Random _random;
        private readonly int _count;
        private readonly int _rounds;
        private readonly int _doubles;
        private readonly int _extra;
        private readonly int[] _categories;
        private readonly int[,] _shares;
        private readonly int[] _totals = new int[4];
        private readonly int[,] _partners;
        private readonly int[,] _opponents;
        private readonly int[][] _positions;
        private readonly Session? _original;
        private readonly PlayerPosition[] _mustMove;
        private readonly bool[][] _fixed;
        private readonly int[][] _baseline;
        private readonly int[,] _accepted;
        private int _score;
        private int _opponentLimit = 2;

        public Search(Player[] players, int courts, int rounds, int seed, Session? original = null, PlayerPosition[]? mustMove = null, bool preserveEdits = false)
        {
            _original = original;
            _mustMove = mustMove ?? [];
            _players = players;
            _count = players.Length;
            _rounds = rounds;
            _random = new(seed);
            _doubles = Math.Min(courts, _count / 4);
            int left = _count - (_doubles * 4);
            _extra = _doubles < courts && left is 2 or 3 ? left : 0;
            _categories = new int[_count];
            for (int slot = _doubles * 4; slot < _count; slot++)
            {
                _categories[slot] = slot < (_doubles * 4) + _extra ? (_extra == 2 ? 1 : 2) : 3;
                _totals[_categories[slot]] += rounds;
            }

            _shares = new int[4, _count];
            _partners = new int[_count, _count];
            _opponents = new int[_count, _count];
            _positions = [.. Enumerable.Range(0, rounds).Select(_ => Enumerable.Range(0, _count).ToArray())];
            _fixed = [.. _positions.Select(row => new bool[row.Length])];
            _accepted = new int[_count, _count];
            if (original is not null)
            {
                for (int r = 0; r < rounds; r++)
                {
                    Round round = original.Rounds[r];
                    Game[] orderedGames = [.. round.Games.OrderByDescending(game => game.Left.Length + game.Right.Length)];
                    Guid[] ids = [.. orderedGames.SelectMany(game => game.Left.Concat(game.Right)), .. round.SittingOut];
                    _positions[r] = [.. ids.Select(id => Array.FindIndex(players, player => player.Id == id))];
                    int slot = 0;
                    foreach (Game game in orderedGames)
                    {
                        foreach (Guid[] side in new[] { game.Left, game.Right })
                        {
                            bool accepted = ReferenceEquals(side, game.Left) ? game.AcceptedLeft : game.AcceptedRight;
                            if (accepted && side.Length == 2)
                            {
                                int first = Array.FindIndex(players, player => player.Id == side[0]);
                                int second = Array.FindIndex(players, player => player.Id == side[1]);
                                _accepted[Math.Min(first, second), Math.Max(first, second)]++;
                            }

                            foreach (Guid id in side)
                            {
                                _fixed[r][slot++] = round.Complete || game.Complete || round.Pinned.Contains(id) || accepted || (preserveEdits && round.Edited.Length > 0);
                            }
                        }
                    }

                    for (; slot < _count; slot++)
                    {
                        _fixed[r][slot] = round.Complete || (preserveEdits && round.Edited.Length > 0);
                    }
                }
            }

            _baseline = [.. _positions.Select(row => row.ToArray())];
        }

        public Round[] Run()
        {
            int bestScore = int.MaxValue;
            int[][] best = [];
            int validScore = int.MaxValue;
            int[][] valid = [];
            int rankMinimum = MinimumRankConflicts() * RankPenalty;
            int opponentMeetings = _rounds * ((_doubles * 4) + (_extra == 3 ? 2 : _extra == 2 ? 1 : 0));
            int minimumRepeats = Math.Max(0, opponentMeetings - (_count * (_count - 1) / 2));

            if (_original is not null)
            {
                _opponentLimit = 3;
                Reset();
                if (_score < rankMinimum + RankPenalty)
                {
                    return _original.Rounds;
                }

                _opponentLimit = 2;
                Reset();
                Round[]? local = Improve();
                if (local is not null)
                {
                    return local;
                }

                Round[]? nearby = Nearby();
                if (nearby is not null)
                {
                    return nearby;
                }
            }

            // Try the stricter opponent limit before allowing the specified fallback of three.
            for (_opponentLimit = 2; _opponentLimit <= 3; _opponentLimit++)
            {
                if (_original is not null && _opponentLimit == 3)
                {
                    Reset();
                    Round[]? nearby = Improve() ?? Nearby();
                    if (nearby is not null)
                    {
                        return nearby;
                    }
                }

                bestScore = int.MaxValue;
                for (int restart = 0; restart < 12; restart++)
                {
                    Reset();
                    const int Iterations = 60000;
                    for (int iteration = 0; iteration < Iterations; iteration++)
                    {
                        int round = _random.Next(_rounds);
                        int first = _random.Next(_count);
                        int second = _random.Next(_count);
                        if (_fixed[round][first] || _fixed[round][second])
                        {
                            continue;
                        }

                        int previous = _score;
                        ApplyRound(round, -1);
                        Swap(round, first, second);
                        ApplyRound(round, 1);
                        double temperature = 150000 * Math.Pow(0.00002, (double)(iteration % 15000) / 15000);
                        if (_score > previous && _random.NextDouble() >= Math.Exp((previous - _score) / temperature))
                        {
                            ApplyRound(round, -1);
                            Swap(round, first, second);
                            ApplyRound(round, 1);
                        }

                        if (_score < bestScore)
                        {
                            bestScore = _score;
                            best = [.. _positions.Select(row => row.ToArray())];
                        }

                        if (_original is null && bestScore == rankMinimum + minimumRepeats)
                        {
                            return Build(best);
                        }
                    }

                    // Keep the best valid result while looking for fewer rank conflicts and repeated opponents.
                    if (bestScore < HardPenalty && bestScore < validScore)
                    {
                        validScore = bestScore;
                        valid = best;
                    }

                    if (bestScore < rankMinimum + RankPenalty)
                    {
                        return Build(best);
                    }
                }
            }

            if (valid.Length > 0)
            {
                return Build(valid);
            }

            throw new InvalidOperationException("Couldn't find a schedule satisfying the rules. Please try again.");
        }

        private Round[]? Nearby()
        {
            Reset();
            List<(int Score, int[][] Rows)> frontier = [(_score, [.. _positions.Select(row => row.ToArray())])];
            int width = _count <= 12 ? 32 : 8;
            for (int depth = 0; depth < 4; depth++)
            {
                var next = new Dictionary<string, (int Score, int[][] Rows)>();
                int bestScore = int.MaxValue;
                int[][]? best = null;
                foreach ((int score, int[][]? rows) in frontier)
                {
                    Load(rows);
                    for (int round = 0; round < _rounds; round++)
                    {
                        for (int first = 0; first < _count; first++)
                        {
                            for (int second = first + 1; second < _count; second++)
                            {
                                if (_fixed[round][first] || _fixed[round][second])
                                {
                                    continue;
                                }

                                ApplyRound(round, -1);
                                Swap(round, first, second);
                                ApplyRound(round, 1);
                                if (_score < (MinimumRankConflicts() * RankPenalty) + RankPenalty && _score < bestScore)
                                {
                                    bestScore = _score;
                                    best = [.. _positions.Select(row => row.ToArray())];
                                }

                                if (next.Count < width || _score < next.Values.Max(item => item.Score))
                                {
                                    string key = string.Join(',', _positions.SelectMany(row => row));
                                    next[key] = (_score, [.. _positions.Select(row => row.ToArray())]);
                                    if (next.Count > width)
                                    {
                                        next.Remove(next.MaxBy(item => item.Value.Score).Key);
                                    }
                                }

                                ApplyRound(round, -1);
                                Swap(round, first, second);
                                ApplyRound(round, 1);
                            }
                        }
                    }
                }

                if (best is not null)
                {
                    return Build(best);
                }

                frontier = [.. next.Values.OrderBy(item => item.Score)];
            }

            return null;
        }

        private void Load(int[][] positions)
        {
            Reset();
            for (int round = 0; round < _rounds; round++)
            {
                ApplyRound(round, -1);
                _positions[round] = [.. positions[round]];
                ApplyRound(round, 1);
            }
        }

        private Round[]? Improve()
        {
            // Try nearby repairs first, avoiding broad changes for a simple manual swap.
            for (int step = 0; step < _rounds * _count; step++)
            {
                int bestScore = _score;
                (int Round, int First, int Second)? best = null;
                for (int round = 0; round < _rounds; round++)
                {
                    for (int first = 0; first < _count; first++)
                    {
                        for (int second = first + 1; second < _count; second++)
                        {
                            if (_fixed[round][first] || _fixed[round][second])
                            {
                                continue;
                            }

                            ApplyRound(round, -1);
                            Swap(round, first, second);
                            ApplyRound(round, 1);
                            if (_score < bestScore)
                            {
                                bestScore = _score;
                                best = (round, first, second);
                            }

                            ApplyRound(round, -1);
                            Swap(round, first, second);
                            ApplyRound(round, 1);
                        }
                    }
                }

                if (best is not { } move)
                {
                    break;
                }

                ApplyRound(move.Round, -1);
                Swap(move.Round, move.First, move.Second);
                ApplyRound(move.Round, 1);
                if (_score < (MinimumRankConflicts() * RankPenalty) + RankPenalty)
                {
                    return Build(_positions);
                }
            }

            return null;
        }

        private int MinimumRankConflicts()
        {
            int ones = _players.Count(player => player.Rank == 1);
            int threes = _players.Count(player => player.Rank == 3);
            return (ones == 1 && threes == _count - 1) || (threes == 1 && ones == _count - 1)
                ? _totals[1] / _count : 0;
        }

        private void Reset()
        {
            Array.Clear(_shares);
            Array.Clear(_partners);
            Array.Clear(_opponents);
            _score = 0;
            for (int category = 1; category <= 3; category++)
            {
                _score += _count * ShareCost(category, 0);
            }

            for (int round = 0; round < _rounds; round++)
            {
                if (_original is null)
                {
                    _random.Shuffle(_positions[round]);
                }
                else
                {
                    _positions[round] = [.. _baseline[round]];
                }

                ApplyRound(round, 1);
            }
        }

        private void Swap(int round, int first, int second)
        {
            (_positions[round][first], _positions[round][second]) = (_positions[round][second], _positions[round][first]);
        }

        private int ShareCost(int category, int value)
        {
            int minimum = _totals[category] / _count;
            int maximum = (_totals[category] + _count - 1) / _count;
            return HardPenalty * (Math.Max(0, minimum - value) + Math.Max(0, value - maximum));
        }

        private int SideAt(int slot)
        {
            int start = _doubles * 4;
            return slot < start ? slot / 2 : _extra == 3 && slot > start && slot < start + 3 ? start + 1 : slot + _count;
        }

        private void ApplyRound(int round, int change)
        {
            int[] row = _positions[round];
            if (_original is not null)
            {
                for (int slot = 0; slot < _count; slot++)
                {
                    int player = row[slot];
                    if (player != _baseline[round][slot])
                    {
                        _score += change * (_original.Rounds[round].Edited.Contains(_players[player].Id) ? 100 : 2);
                    }

                    foreach (PlayerPosition move in _mustMove)
                    {
                        if (move.Round == round && _players[player].Id == move.Player && SideAt(slot) == SideAt(Array.IndexOf(_baseline[round], player)))
                        {
                            _score += change * HardPenalty;
                        }
                    }
                }
            }

            for (int slot = _doubles * 4; slot < _count; slot++)
            {
                int category = _categories[slot];
                int player = row[slot];
                _score -= ShareCost(category, _shares[category, player]);
                _shares[category, player] += change;
                _score += ShareCost(category, _shares[category, player]);
            }

            for (int game = 0; game < _doubles; game++)
            {
                int offset = game * 4;
                Pair(_partners, row[offset], row[offset + 1], change, 1);
                Pair(_partners, row[offset + 2], row[offset + 3], change, 1);
                for (int left = 0; left < 2; left++)
                {
                    for (int right = 2; right < 4; right++)
                    {
                        Pair(_opponents, row[offset + left], row[offset + right], change, _opponentLimit);
                    }
                }
            }

            int extraOffset = _doubles * 4;
            if (_extra == 3)
            {
                Pair(_partners, row[extraOffset + 1], row[extraOffset + 2], change, 1);
                Pair(_opponents, row[extraOffset], row[extraOffset + 1], change, _opponentLimit);
                Pair(_opponents, row[extraOffset], row[extraOffset + 2], change, _opponentLimit);
            }
            else if (_extra == 2)
            {
                int first = row[extraOffset];
                int second = row[extraOffset + 1];
                Pair(_opponents, first, second, change, _opponentLimit);
                if (IsRankConflict(first, second))
                {
                    _score += change * RankPenalty;
                }
            }
        }

        private void Pair(int[,] counts, int first, int second, int change, int limit)
        {
            if (first > second)
            {
                (first, second) = (second, first);
            }

            if (ReferenceEquals(counts, _partners))
            {
                limit += _accepted[first, second];
            }

            int previous = counts[first, second];
            counts[first, second] += change;
            _score += HardPenalty * (Math.Max(0, counts[first, second] - limit) - Math.Max(0, previous - limit));
            if (ReferenceEquals(counts, _opponents))
            {
                _score += Math.Max(0, counts[first, second] - 1) - Math.Max(0, previous - 1);
            }
        }

        private bool IsRankConflict(int first, int second)
        {
            return (_players[first].Rank == 1 && _players[second].Rank == 3) ||
                (_players[first].Rank == 3 && _players[second].Rank == 1);
        }

        private Round[] Build(int[][] positions)
        {
            return [.. positions.Select((row, roundIndex) =>
            {
                List<Game> games = [];
                for (int game = 0; game < _doubles; game++)
                {
                    int offset = game * 4;
                    games.Add(new(
                        [_players[row[offset]].Id, _players[row[offset + 1]].Id],
                        [_players[row[offset + 2]].Id, _players[row[offset + 3]].Id]));
                }

                int start = _doubles * 4;
                if (_extra > 0)
                {
                    games.Add(new(
                        [_players[row[start]].Id],
                        [.. row.Skip(start + 1).Take(_extra - 1).Select(player => _players[player].Id)],
                        _extra == 2 && IsRankConflict(row[start], row[start + 1])));
                }

                var result = new Round([.. games], [.. row.Skip(start + _extra).Select(player => _players[player].Id)]);
                if (_original is null)
                {
                    return result;
                }

                Round original = _original.Rounds[roundIndex];
                Game[] originalGames = [.. original.Games.OrderByDescending(game => game.Left.Length + game.Right.Length)];
                Guid[] moved = [.. row.Where((player, slot) => player != _baseline[roundIndex][slot]).Select(player => _players[player].Id)];
                return result with
                {
                    Complete = original.Complete,
                    Pinned = original.Pinned,
                    Edited = original.Edited,
                    Moved = [.. original.Moved.Union(moved)],
                    Games = [.. result.Games.Select((game, index) => (Game: game with
                    {
                        Complete = originalGames[index].Complete,
                        AcceptedLeft = originalGames[index].AcceptedLeft,
                        AcceptedRight = originalGames[index].AcceptedRight,
                    }, Index: Array.IndexOf(original.Games, originalGames[index]))).OrderBy(item => item.Index).Select(item => item.Game)],
                };
            })];
        }
    }
}
