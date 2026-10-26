namespace SocialTennis.Scheduling;

/// <summary>A constrained repair outcome; a failed repair retains the original session.</summary>
/// <param name="Session">The repaired or unchanged saved session.</param>
/// <param name="Succeeded">Whether the schedule rules could be satisfied.</param>
public sealed record ScheduleRepairResult(Session Session, bool Succeeded);
