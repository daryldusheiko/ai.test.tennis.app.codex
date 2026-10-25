namespace SocialTennis.Scheduling;

/// <summary>Persisted setup progression.</summary>
public enum ScheduleStep
{
    /// <summary>Select courts.</summary>
    Courts,

    /// <summary>Select players.</summary>
    Players,

    /// <summary>Review generated rounds.</summary>
    Review,
}
