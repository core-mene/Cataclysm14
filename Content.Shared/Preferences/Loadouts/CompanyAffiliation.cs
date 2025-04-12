using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Represents the company affiliation choice for a character.
/// </summary>
[Serializable, NetSerializable]
public enum CompanyAffiliation
{
    Neutral = 0,
    Rogue = 1
} 