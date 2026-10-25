namespace SocialTennis.Players;

/// <summary>A player retained on this device.</summary>
/// <param name="Id">Stable player identity.</param>
/// <param name="FirstName">Required given name.</param>
/// <param name="LastName">Optional family name.</param>
/// <param name="Rank">Optional playing rank.</param>
/// <param name="Comment">Private organiser notes.</param>
public sealed record Player(Guid Id, string FirstName, string LastName, int? Rank, string Comment)
{
    /// <summary>Gets the display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
