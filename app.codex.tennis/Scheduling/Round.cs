namespace SocialTennis.Scheduling;

/// <summary>A session round.</summary>
/// <param name="Games">Ordered games.</param>
/// <param name="SittingOut">Players without a game.</param>
/// <param name="Complete">Whether the round is complete.</param>
public sealed record Round(Game[] Games, Guid[] SittingOut, bool Complete = false)
{
    /// <summary>Gets the player identities fixed in this round.</summary>
    public Guid[] Pinned { get; init => field = value ?? []; } = [];

    /// <summary>Gets the players shown in blue.</summary>
    public Guid[] Moved { get; init => field = value ?? []; } = [];

    /// <summary>Gets the players manually moved by the user.</summary>
    public Guid[] Edited { get; init => field = value ?? []; } = [];
}
