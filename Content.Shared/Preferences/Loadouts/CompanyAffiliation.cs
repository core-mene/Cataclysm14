using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Represents the company affiliation choice for a character.
/// </summary>
[Serializable, NetSerializable]
public enum CompanyAffiliation
{
    None = 0,
    Custom = 1  // New value for custom companies
}

/// <summary>
/// Stores information about custom companies created by players.
/// </summary>
[Serializable, NetSerializable]
public sealed class CustomCompanyData
{
    /// <summary>
    /// The name of the custom company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The username of the player who created the company.
    /// </summary>
    public string CreatorUsername { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new custom company data instance.
    /// </summary>
    public CustomCompanyData() { }

    /// <summary>
    /// Creates a new custom company data with the specified name.
    /// </summary>
    /// <param name="name">The name of the custom company.</param>
    public CustomCompanyData(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new custom company data with the specified name and creator.
    /// </summary>
    /// <param name="name">The name of the custom company.</param>
    /// <param name="creatorUsername">The username of the creator.</param>
    public CustomCompanyData(string name, string creatorUsername)
    {
        Name = name;
        CreatorUsername = creatorUsername;
    }
}
