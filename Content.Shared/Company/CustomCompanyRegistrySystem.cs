using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Content.Shared.Preferences.Loadouts;

namespace Content.Shared.Company;

/// <summary>
/// This system manages a registry of custom companies that are available to all players.
/// </summary>
public abstract class SharedCustomCompanyRegistrySystem : EntitySystem
{
    [Dependency] protected readonly INetManager NetManager = default!;
    
    // Registry of all custom companies by unique ID
    protected Dictionary<string, CustomCompanyData> CustomCompanies = new();
    
    public override void Initialize()
    {
        base.Initialize();
    }
    
    /// <summary>
    /// Gets all custom companies in the registry
    /// </summary>
    public IReadOnlyDictionary<string, CustomCompanyData> GetAllCustomCompanies()
    {
        return CustomCompanies;
    }
    
    /// <summary>
    /// Checks if a custom company exists in the registry
    /// </summary>
    public bool CustomCompanyExists(string companyName)
    {
        return CustomCompanies.ContainsKey(companyName.ToLowerInvariant());
    }
    
    /// <summary>
    /// Checks if a company name would be a duplicate of a preset or existing custom company
    /// </summary>
    /// <param name="companyName">The company name to check</param>
    /// <returns>True if the name would be a duplicate, false otherwise</returns>
    public bool IsDuplicateCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return false;
            
        var normalizedName = companyName.ToLowerInvariant();
        
        // Check against preset company names
        if (normalizedName == "none")
            return true;
            
        // Check against existing custom companies
        return CustomCompanies.ContainsKey(normalizedName);
    }
    
    /// <summary>
    /// Gets a custom company from the registry
    /// </summary>
    public CustomCompanyData? GetCustomCompany(string companyName)
    {
        if (CustomCompanyExists(companyName))
            return CustomCompanies[companyName.ToLowerInvariant()];
            
        return null;
    }
}

/// <summary>
/// Network message for adding a custom company to the registry
/// </summary>
[Serializable, NetSerializable]
public sealed class CustomCompanyRegistryMessage : EntityEventArgs
{
    public CustomCompanyData CompanyData { get; }
    
    public CustomCompanyRegistryMessage(CustomCompanyData companyData)
    {
        CompanyData = companyData;
    }
}

/// <summary>
/// Network message for deleting a custom company from the registry
/// </summary>
[Serializable, NetSerializable]
public sealed class CustomCompanyDeletedMessage : EntityEventArgs
{
    public string CompanyName { get; }
    
    public CustomCompanyDeletedMessage(string companyName)
    {
        CompanyName = companyName;
    }
} 