namespace SocialTennis.Players;

/// <summary>Defines player list ordering.</summary>
public enum PlayerSort
{
    /// <summary>First names A to Z, then last names A to Z.</summary>
    FirstNameAscending,

    /// <summary>First names Z to A, then last names Z to A.</summary>
    FirstNameDescending,

    /// <summary>Last names A to Z, then first names A to Z.</summary>
    LastNameAscending,

    /// <summary>Last names Z to A, then first names Z to A.</summary>
    LastNameDescending,
}
