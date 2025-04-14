using Content.Shared.Company;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Client.Company;

/// <summary>
/// Client-side implementation of the custom company registry.
/// Receives custom companies from the server and notifies UI components.
/// </summary>
public sealed class CustomCompanyRegistrySystem : SharedCustomCompanyRegistrySystem
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    /// Event raised when a new custom company is added to the registry
    /// </summary>
    public event Action<CustomCompanyData>? CustomCompanyAdded;

    /// <summary>
    /// Event raised when a custom company is deleted from the registry
    /// </summary>
    public event Action<string>? CustomCompanyDeleted;

    /// <summary>
    /// Tracks whether the current player has already created a custom company
    /// </summary>
    public bool HasCreatedCustomCompany { get; private set; }

    /// <summary>
    /// The name of the custom company created by the player
    /// </summary>
    public string? PlayerCreatedCompanyName { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("company.registry");

        SubscribeNetworkEvent<CustomCompanyRegistryMessage>(OnCustomCompanyMessage);
        SubscribeNetworkEvent<CustomCompanyDeletedMessage>(OnCustomCompanyDeleted);

        _sawmill.Debug("CustomCompanyRegistrySystem initialized");
    }

    /// <summary>
    /// Request the server to add a new custom company
    /// </summary>
    public void RequestAddCustomCompany(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return;

        // Check if it would be a duplicate
        if (IsDuplicateCompanyName(companyName))
        {
            _sawmill.Debug($"Custom company '{companyName}' would be a duplicate, not requesting from server");
            return;
        }

        // Create the company data (server will set the creator username)
        var companyData = new CustomCompanyData(companyName);

        _sawmill.Debug($"Requesting server to add custom company: {companyName}");

        // Mark that this player has created a custom company
        HasCreatedCustomCompany = true;
        PlayerCreatedCompanyName = companyName;

        // Send to server to broadcast to all clients
        RaiseNetworkEvent(new CustomCompanyRegistryMessage(companyData));
    }

    /// <summary>
    /// Request the server to delete a custom company created by this player
    /// </summary>
    /// <param name="companyName">Optional specific company name to delete. If null, uses PlayerCreatedCompanyName.</param>
    public void RequestDeleteCustomCompany(string? companyName = null)
    {
        // Use provided name or fallback to player's created company
        companyName = companyName ?? PlayerCreatedCompanyName;

        // Ensure there's a company to delete
        if (string.IsNullOrWhiteSpace(companyName))
        {
            _sawmill.Debug("Cannot delete company - no company name specified");
            return;
        }

        // Check if this is the player's own company (only if no specific name provided)
        if (companyName == PlayerCreatedCompanyName && !HasCreatedCustomCompany)
        {
            _sawmill.Debug("Cannot delete company - player has not created one");
            return;
        }

        // Check if the company exists in our registry
        if (!CustomCompanyExists(companyName))
        {
            _sawmill.Debug($"Cannot delete company '{companyName}' - not found in registry");
            return;
        }

        _sawmill.Debug($"Requesting server to delete custom company: {companyName}");

        // Reset player's created company status if this is the player's company
        if (companyName == PlayerCreatedCompanyName)
        {
            HasCreatedCustomCompany = false;
            PlayerCreatedCompanyName = null;
        }

        // Remove from local registry preemptively to avoid UI issues
        var key = companyName.ToLowerInvariant();
        if (CustomCompanies.ContainsKey(key))
        {
            CustomCompanies.Remove(key);
        }

        // Send deletion request to server
        RaiseNetworkEvent(new CustomCompanyDeletedMessage(companyName));

        // Notify subscribers locally immediately
        _sawmill.Debug($"Invoking CustomCompanyDeleted event for {companyName}");
        CustomCompanyDeleted?.Invoke(companyName);
    }

    private void OnCustomCompanyMessage(CustomCompanyRegistryMessage message)
    {
        var companyData = message.CompanyData;
        var key = companyData.Name.ToLowerInvariant();

        _sawmill.Debug($"Received custom company from server: {companyData.Name}");

        // Check if this company is created by the current player
        var playerName = _cfg.GetCVar(CVars.PlayerName);
        if (companyData.CreatorUsername == playerName)
        {
            HasCreatedCustomCompany = true;
            PlayerCreatedCompanyName = companyData.Name;
            _sawmill.Debug($"Marked {companyData.Name} as player's created company");
        }

        // Add to local registry
        CustomCompanies[key] = companyData;

        // Notify subscribers
        _sawmill.Debug($"Invoking CustomCompanyAdded event for {companyData.Name}");
        CustomCompanyAdded?.Invoke(companyData);
    }

    private void OnCustomCompanyDeleted(CustomCompanyDeletedMessage message)
    {
        var companyName = message.CompanyName;
        var key = companyName.ToLowerInvariant();

        _sawmill.Debug($"Received company deletion from server: {companyName}");

        // Check if this was the player's created company
        if (HasCreatedCustomCompany && PlayerCreatedCompanyName?.ToLowerInvariant() == key)
        {
            HasCreatedCustomCompany = false;
            PlayerCreatedCompanyName = null;
            _sawmill.Debug($"Reset player's created company status for {companyName}");
        }

        // Check if we have this company in our registry
        if (!CustomCompanies.ContainsKey(key))
        {
            _sawmill.Warning($"Received deletion for unknown company: {companyName}");
            return;
        }

        // Remove from local registry
        CustomCompanies.Remove(key);

        // Notify subscribers
        _sawmill.Debug($"Invoking CustomCompanyDeleted event for {companyName}");
        CustomCompanyDeleted?.Invoke(companyName);
    }
}
