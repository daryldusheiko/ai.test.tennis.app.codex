namespace SocialTennis.Scheduling;

/// <summary>Isolates device session persistence.</summary>
public interface IScheduleStorage
{
    /// <summary>Loads the saved session, or null when none exists.</summary>
    /// <returns>The saved session.</returns>
    Task<Session?> LoadAsync();

    /// <summary>Atomically saves the session.</summary>
    /// <param name="session">The session to save.</param>
    /// <returns>The save operation.</returns>
    Task SaveAsync(Session session);
}
