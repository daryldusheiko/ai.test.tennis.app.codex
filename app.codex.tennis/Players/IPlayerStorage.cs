namespace SocialTennis.Players;

/// <summary>Defines the player storage boundary.</summary>
public interface IPlayerStorage
{
    /// <summary>Loads the saved player document.</summary>
    /// <returns>The saved document.</returns>
    Task<PlayerData> LoadAsync();

    /// <summary>Saves the complete player document.</summary>
    /// <param name="data">The complete document to save.</param>
    /// <returns>The save operation.</returns>
    Task SaveAsync(PlayerData data);
}
