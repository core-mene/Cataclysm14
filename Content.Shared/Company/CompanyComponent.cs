using Robust.Shared.GameStates;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Serialization;

namespace Content.Shared.Company;

/// <summary>
/// Stores the company affiliation and custom name for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CompanyComponent : Component
{
    /// <summary>
    /// The company affiliation of this entity.
    /// </summary>
    [DataField]
    public CompanyAffiliation Company = CompanyAffiliation.None;
    
    /// <summary>
    /// The name of the custom company, if Company is set to CustomCompany.
    /// </summary>
    [DataField]
    public string? CustomCompanyName = null;
}

[Serializable, NetSerializable]
public sealed class CompanyComponentState : ComponentState
{
    public CompanyAffiliation Company { get; }
    public string? CustomCompanyName { get; }

    public CompanyComponentState(CompanyAffiliation company, string? customCompanyName)
    {
        Company = company;
        CustomCompanyName = customCompanyName;
    }
} 