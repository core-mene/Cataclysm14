using Content.Shared.Company;
using Content.Shared.Preferences.Loadouts;
using Robust.Client.GameObjects;
using Content.Shared.Examine;
using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using System.Globalization;
using Robust.Shared.Maths;

namespace Content.Client.Company;

/// <summary>
/// Client-side implementation of the company system.
/// </summary>
public sealed class ClientCompanySystem : SharedCompanySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CompanyComponent, ComponentHandleState>(OnHandleState);
    }

    /// <summary>
    /// Gets a display name for a company, including the custom company name for CustomCompany.
    /// </summary>
    /// <param name="uid">The entity to get the company name for</param>
    /// <returns>A formatted company name suitable for display</returns>
    public string GetCompanyDisplayName(EntityUid uid)
    {
        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return "None";

        // If the company type is Custom but there's no custom company name, 
        // it should be treated as None (happens when a custom company is deleted)
        if (companyComp.Company == CompanyAffiliation.Custom)
        {
            if (!string.IsNullOrEmpty(companyComp.CustomCompanyName))
            {
                return companyComp.CustomCompanyName;
            }
            else
            {
                // Company is marked as Custom but has no name - should be None
                return "None";
            }
        }

        return companyComp.Company.ToString();
    }
    
    /// <summary>
    /// Formats a company name with deterministic color based on the name.
    /// </summary>
    /// <param name="companyName">The company name to format</param>
    /// <returns>A formatted company name with color markup</returns>
    public string GetFormattedCompanyName(string companyName)
    {
        if (string.IsNullOrEmpty(companyName) || companyName == "None")
            return companyName;
        
        // Apply title casing for display purposes only
        var displayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(companyName.ToLowerInvariant());
        
        // Use the shared color helper with the original name to ensure consistent colors
        var color = CompanyColorHelper.GetDeterministicColor(companyName);
        return CompanyColorHelper.ColorText(displayName, color);
    }

    private void OnHandleState(EntityUid uid, CompanyComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CompanyComponentState state)
            return;

        component.Company = state.Company;
        component.CustomCompanyName = state.CustomCompanyName;
    }
}
