using Content.Shared.Preferences.Loadouts;

namespace Content.Shared.Company;

/// <summary>
/// This system provides shared functionality for interacting with the company component.
/// </summary>
public abstract class SharedCompanySystem : EntitySystem
{
    /// <summary>
    /// Tries to get the company affiliation of an entity.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="company">The entity's company, if any</param>
    /// <returns>True if the entity has a company component, false otherwise</returns>
    public bool TryGetCompany(EntityUid uid, out CompanyAffiliation company)
    {
        company = CompanyAffiliation.None;

        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return false;

        company = companyComp.Company;
        return true;
    }

    /// <summary>
    /// Tries to get the company name of an entity, including custom company names.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="companyName">The entity's company name</param>
    /// <returns>True if the entity has a company component, false otherwise</returns>
    public bool TryGetCompanyName(EntityUid uid, out string companyName)
    {
        companyName = "None";

        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return false;

        // Handle Custom company with a valid name
        if (companyComp.Company == CompanyAffiliation.Custom && !string.IsNullOrEmpty(companyComp.CustomCompanyName))
        {
            companyName = companyComp.CustomCompanyName;
            return true;
        }
        
        // If it's a Custom company with no name, treat it as None
        if (companyComp.Company == CompanyAffiliation.Custom)
        {
            companyName = "None";
            return true;
        }

        companyName = companyComp.Company.ToString();
        return true;
    }

    /// <summary>
    /// Gets the company affiliation of an entity, or returns None if none exists.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <returns>The entity's company or None if none exists</returns>
    public CompanyAffiliation GetCompanyOrDefault(EntityUid uid)
    {
        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return CompanyAffiliation.None;

        return companyComp.Company;
    }

    /// <summary>
    /// Checks if an entity belongs to a specific company.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="company">The company to check for</param>
    /// <returns>True if the entity belongs to the specified company</returns>
    public bool IsCompanyMember(EntityUid uid, CompanyAffiliation company)
    {
        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return false;

        return companyComp.Company == company;
    }
}
