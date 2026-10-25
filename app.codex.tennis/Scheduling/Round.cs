namespace SocialTennis.Scheduling;

/// <summary>A session round.</summary>
/// <param name="Games">Ordered games.</param>
/// <param name="SittingOut">Players without a game.</param>
/// <param name="Complete">Whether the round is complete.</param>
public sealed record Round(Game[] Games, Guid[] SittingOut, bool Complete = false);
