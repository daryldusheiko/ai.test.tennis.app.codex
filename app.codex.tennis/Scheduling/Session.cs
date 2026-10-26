using SocialTennis.Players;

namespace SocialTennis.Scheduling;

/// <summary>The complete versioned session, including the saved setup step.</summary>
/// <param name="Version">Schema version.</param>
/// <param name="Step">Current setup page.</param>
/// <param name="Courts">Sorted court numbers.</param>
/// <param name="Players">Player snapshots retained independently of the player list.</param>
/// <param name="Rounds">Ordered rounds.</param>
/// <param name="InPlay">Whether Play has been pressed.</param>
public sealed record Session(int Version, ScheduleStep Step, int[] Courts, Player[] Players, Round[] Rounds, bool InPlay)
{
    /// <summary>Gets a value indicating whether player editing is active.</summary>
    public bool Editing { get; init; }

    /// <summary>Gets a new court-selection session.</summary>
    public static Session Empty => new(1, ScheduleStep.Courts, [], [], [], false);
}
