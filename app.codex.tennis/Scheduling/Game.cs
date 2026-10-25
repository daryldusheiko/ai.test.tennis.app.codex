namespace SocialTennis.Scheduling;

/// <summary>A scheduled match.</summary>
/// <param name="Left">Left side player identities.</param>
/// <param name="Right">Right side player identities.</param>
/// <param name="RankConflict">Whether singles ranks conflict.</param>
/// <param name="Complete">Whether the game is complete.</param>
public sealed record Game(Guid[] Left, Guid[] Right, bool RankConflict = false, bool Complete = false);
