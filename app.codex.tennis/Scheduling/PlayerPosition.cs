namespace SocialTennis.Scheduling;

/// <summary>A player selected in one round, including sitting-out positions.</summary>
public sealed record PlayerPosition(int Round, Guid Player);
