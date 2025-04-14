using System.Globalization;
using Content.Shared.Company;
using Content.Shared.GameTicking;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Examine;
using System.Numerics; // Required for Color.FromHsv
using Robust.Shared.Utility; // Required for Markup
using Robust.Shared.GameStates;
using Robust.Shared.Random; // Required for deterministic color generation seeding if needed, though hash is simpler
using Robust.Shared.Graphics; // Required for Color
using Robust.Shared.Prototypes; // Required for Loc implicitly? Check if needed.
using Robust.Shared.Timing; // Potentially needed if using time-based seeding, but hash is better

namespace Content.Server.Company;

/// <summary>
/// System that adds the CompanyComponent to players when they join the round and handles state.
/// </summary>
public sealed class CompanySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    [Dependency] private readonly IRobustRandom _random = default!; // Added for potential color variation, though hash is primary

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<CompanyExamineComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CompanyComponent, ComponentGetState>(OnGetState);
    }

    // When a player spawns, add the CompanyComponent with their selected company
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Get their selected company from their profile
        var company = args.Profile.Company;

        // Add the company component
        var companyComp = EnsureComp<CompanyComponent>(args.Mob);
        companyComp.Company = company;

        // Handle custom company names
        if (company == CompanyAffiliation.Custom && args.Profile.CustomCompanyData != null)
        {
            companyComp.CustomCompanyName = args.Profile.CustomCompanyData.Name;
        }

        // Add the examine component to show company info during examination
        EnsureComp<CompanyExamineComponent>(args.Mob);

        // Apply any company-specific effects if needed
        // (none for now, but can be extended)

        Dirty(args.Mob, companyComp);
    }

    private void OnGetState(EntityUid uid, CompanyComponent component, ref ComponentGetState args)
    {
        // Apply title casing here to ensure the state sent to the client is correct from the start
        string companyName;
        
        // Handle custom company with a valid name
        if (component.Company == CompanyAffiliation.Custom && !string.IsNullOrEmpty(component.CustomCompanyName))
        {
            companyName = component.CustomCompanyName;
        }
        // If it's a custom company but with no name, treat as None
        else if (component.Company == CompanyAffiliation.Custom)
        {
            companyName = "None";
            
            // If the company type is Custom but the name is null, we should correct it to be None
            // This ensures consistent state on the client
            component.Company = CompanyAffiliation.None;
            Dirty(uid, component);
        }
        else
        {
            companyName = component.Company.ToString();
        }

        if (!string.IsNullOrEmpty(companyName))
            companyName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(companyName.ToLowerInvariant());

        // Send the potentially title-cased name in the state
        args.State = new CompanyComponentState(component.Company, companyName);
    }

    private void OnExamined(EntityUid uid, CompanyExamineComponent component, ExaminedEvent args)
    {
        if (!TryComp<CompanyComponent>(uid, out var companyComp))
            return;

        string companyName;

        // Handle custom company with a valid name
        if (companyComp.Company == CompanyAffiliation.Custom && !string.IsNullOrEmpty(companyComp.CustomCompanyName))
        {
            companyName = companyComp.CustomCompanyName;
        }
        // If it's a custom company but with no name, treat as None
        else if (companyComp.Company == CompanyAffiliation.Custom)
        {
            companyName = "None";
        }
        else
        {
            companyName = companyComp.Company.ToString();
        }

        // Apply title casing for display purposes only
        var displayName = string.IsNullOrEmpty(companyName) 
            ? companyName 
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(companyName.ToLowerInvariant());

        // Use the shared color helper with the original name to ensure consistent colors
        var color = CompanyColorHelper.GetDeterministicColor(companyName);
        var coloredCompanyName = CompanyColorHelper.ColorText(displayName, color);

        args.PushMarkup(Loc.GetString("company-examine", ("company", (object)coloredCompanyName))); // Cast to object to resolve CS1503
    }
}
