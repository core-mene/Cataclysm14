using Content.Shared.Company;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.Company;

/// <summary>
/// This system handles assigning a company to players when they join.
/// </summary>
public sealed class CompanySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to player spawn event to add the company component
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        
        // Subscribe to examination to show the company on examine
        SubscribeLocalEvent<CompanyComponent, ExaminedEvent>(OnExamined);
    }
    
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Add the company component with the player's saved company
        var companyComp = EnsureComp<CompanyComponent>(args.Mob);
        
        // Get the company from the player's profile
        var company = args.Profile.Company;
        
        // Set the company name directly from the profile
        companyComp.CompanyName = company;
        
        // Ensure the component is networked to clients
        Dirty(args.Mob, companyComp);
    }
    
    private void OnExamined(EntityUid uid, CompanyComponent component, ExaminedEvent args)
    {
        // Try to get the prototype for the company
        if (_prototypeManager.TryIndex<CompanyPrototype>(component.CompanyName, out var prototype))
        {
            // Use the color from the prototype
            args.PushMarkup($"Company: [color={prototype.Color.ToHex()}]{prototype.Name}[/color]");
        }
        else
        {
            // Fallback for companies without prototypes
            args.PushMarkup($"Company: [color=yellow]{component.CompanyName}[/color]");
        }
    }
} 