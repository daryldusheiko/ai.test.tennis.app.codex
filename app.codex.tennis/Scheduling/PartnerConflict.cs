namespace SocialTennis.Scheduling;

/// <summary>A repeat partnership involving a completed game.</summary>
public sealed record PartnerConflict(int CompletedRound, int Round, int Game, bool Left, Guid First, Guid Second);
