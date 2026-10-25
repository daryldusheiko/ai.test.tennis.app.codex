namespace SocialTennis.Scheduling;

/// <summary>The result of court validation.</summary>
/// <param name="Courts">Parsed court numbers.</param>
/// <param name="Error">Validation error, if any.</param>
public sealed record CourtSelection(int[] Courts, string? Error);
