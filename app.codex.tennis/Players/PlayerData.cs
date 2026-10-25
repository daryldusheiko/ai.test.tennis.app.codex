namespace SocialTennis.Players;

/// <summary>The versioned player storage document.</summary>
/// <param name="Version">Storage schema version.</param>
/// <param name="Players">Saved players.</param>
/// <param name="Sort">Remembered list order.</param>
public sealed record PlayerData(int Version, Player[] Players, PlayerSort Sort)
{
    /// <summary>Gets the initial empty document.</summary>
    public static PlayerData Empty => new(1, [], PlayerSort.FirstNameAscending);
}
