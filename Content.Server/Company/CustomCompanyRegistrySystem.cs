using Content.Shared.Company;
using Content.Shared.Preferences.Loadouts;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;

namespace Content.Server.Company;

/// <summary>
/// Server-side implementation of the custom company registry.
/// </summary>
public sealed class CustomCompanyRegistrySystem : SharedCustomCompanyRegistrySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("company.registry");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeNetworkEvent<CustomCompanyRegistryMessage>(OnCustomCompanyMessage);
        SubscribeNetworkEvent<CustomCompanyDeletedMessage>(OnCustomCompanyDeleteRequest);

        // Register admin command
        _consoleHost.RegisterCommand("deletecompany",
            "Deletes a custom company and resets all players in it to Neutral",
            "deletecompany <company name>",
            DeleteCompanyCommand);

        _sawmill.Debug("Server CustomCompanyRegistrySystem initialized");
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <summary>
    /// Handle custom company messages from clients
    /// </summary>
    private void OnCustomCompanyMessage(CustomCompanyRegistryMessage message, EntitySessionEventArgs args)
    {
        var companyData = message.CompanyData;
        
        // Get the username of the player who created the company
        var username = args.SenderSession?.Name ?? "Unknown";
        
        // Update the company data with the creator's username
        companyData = new CustomCompanyData(companyData.Name, username);

        _sawmill.Debug($"Received custom company request from client {username}: {companyData.Name}");

        // Add the company to the registry and broadcast to all
        AddCustomCompany(companyData);
    }

    /// <summary>
    /// Adds a custom company to the registry and broadcasts it to all clients
    /// </summary>
    public void AddCustomCompany(CustomCompanyData companyData)
    {
        var key = companyData.Name.ToLowerInvariant();

        // Check if this would create a duplicate of a preset company
        if (key == "none")
        {
            _sawmill.Warning($"Rejected custom company '{companyData.Name}' that matches a preset company name");
            return;
        }

        // Check if company already exists in registry
        if (CustomCompanies.ContainsKey(key))
        {
            _sawmill.Debug($"Custom company '{companyData.Name}' already exists, not adding duplicate");
            return;
        }

        // Add to registry
        _sawmill.Debug($"Adding custom company to registry: {companyData.Name}");
        CustomCompanies[key] = companyData;

        // Broadcast to all clients
        RaiseNetworkEvent(new CustomCompanyRegistryMessage(companyData));
    }

    /// <summary>
    /// Deletes a custom company from the registry and resets all players in it to Neutral
    /// </summary>
    /// <param name="companyName">The name of the company to delete</param>
    /// <returns>True if the company was found and deleted, false otherwise</returns>
    public bool DeleteCustomCompany(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return false;

        var key = companyName.ToLowerInvariant();

        // Check if this is a preset company (which can't be deleted)
        if (key == "none")
        {
            _sawmill.Warning($"Cannot delete preset company '{companyName}'");
            return false;
        }

        // Check if company exists in registry
        if (!CustomCompanies.ContainsKey(key))
        {
            _sawmill.Debug($"Custom company '{companyName}' not found, cannot delete");
            return false;
        }

        // Remove from registry
        _sawmill.Debug($"Removing custom company from registry: {companyName}");
        CustomCompanies.Remove(key);

        // Reset all players with this company to None
        ResetPlayersWithCustomCompany(companyName);

        // Broadcast deletion to all clients with explicit Filter.Broadcast
        var deleteMsg = new CustomCompanyDeletedMessage(companyName);
        RaiseNetworkEvent(deleteMsg, Filter.Broadcast());
        _sawmill.Debug($"Broadcast deletion of company '{companyName}' to all clients");

        return true;
    }

    /// <summary>
    /// Resets all players with the specified custom company to None
    /// </summary>
    private void ResetPlayersWithCustomCompany(string companyName)
    {
        var companySystem = EntitySystem.Get<CompanySystem>();

        foreach (var player in _playerManager.Sessions)
        {
            // Check if the player has an entity with the custom company and reset that entity
            if (player.AttachedEntity is { } playerEntity)
            {
                if (!EntityManager.TryGetComponent<CompanyComponent>(playerEntity, out var companyComp))
                    continue;

                // Check if the player is in the custom company being deleted
                if (companyComp.Company == CompanyAffiliation.Custom &&
                    companyComp.CustomCompanyName?.ToLowerInvariant() == companyName.ToLowerInvariant())
                {
                    // Reset to None
                    _sawmill.Debug($"Resetting player {player.Name} from custom company '{companyName}' to None");
                    companyComp.Company = CompanyAffiliation.None;
                    companyComp.CustomCompanyName = null;

                    // Mark as dirty to network the change
                    EntityManager.Dirty(playerEntity, companyComp);
                }
            }

            // Also update the player's preferences if they're loaded
            if (!_prefsManager.HavePreferencesLoaded(player))
                continue;
                
            try
            {
                // Get player preferences
                var userPrefs = _prefsManager.GetPreferences(player.UserId);
                    
                // Check all character profiles
                bool needsUpdate = false;
                var characters = new Dictionary<int, ICharacterProfile>(userPrefs.Characters);
                
                foreach (var (slot, character) in userPrefs.Characters)
                {
                    // Only process humanoid characters
                    if (character is not HumanoidCharacterProfile profile)
                        continue;
                    
                    // Check if this profile uses the deleted company
                    if (profile.Company == CompanyAffiliation.Custom && 
                        profile.CustomCompanyData?.Name.ToLowerInvariant() == companyName.ToLowerInvariant())
                    {
                        // Reset to None and update the preferences
                        _sawmill.Debug($"Resetting character profile #{slot} for player {player.Name} from custom company '{companyName}' to None");
                        characters[slot] = profile.WithCompany(CompanyAffiliation.None, null);
                        needsUpdate = true;
                    }
                }
                
                // If any profiles were updated, save the changes
                if (needsUpdate)
                {
                    // Create new preferences with updated characters
                    var newPrefs = new PlayerPreferences(
                        characters, 
                        userPrefs.SelectedCharacterIndex, 
                        userPrefs.AdminOOCColor
                    );
                    
                    // Queue a task to update each changed slot
                    foreach (var (slot, profile) in characters)
                    {
                        if (userPrefs.Characters[slot] != profile)
                        {
                            // Kick off an async task to save the profile
                            _ = _prefsManager.SetProfile(player.UserId, slot, profile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error updating preferences for player {player.Name}: {ex}");
            }
        }
    }

    /// <summary>
    /// Handle custom company deletion messages from clients
    /// </summary>
    private void OnCustomCompanyDeleteRequest(CustomCompanyDeletedMessage message, EntitySessionEventArgs args)
    {
        var companyName = message.CompanyName;
        var session = args.SenderSession;
        var username = session?.Name ?? "Unknown";
        
        _sawmill.Debug($"Received custom company deletion request from client {username}: {companyName}");
        
        if (string.IsNullOrWhiteSpace(companyName))
            return;
            
        // Check if this company exists
        var key = companyName.ToLowerInvariant();
        if (!CustomCompanies.TryGetValue(key, out var companyData))
        {
            _sawmill.Warning($"Client {username} tried to delete non-existent company: {companyName}");
            return;
        }
        
        // Security check: only let users delete their own companies
        if (companyData.CreatorUsername != username)
        {
            _sawmill.Warning($"Client {username} tried to delete company {companyName} created by {companyData.CreatorUsername}");
            return;
        }
        
        // Delete the company
        DeleteCustomCompany(companyName);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void DeleteCompanyCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Expected exactly one argument: the company name to delete");
            return;
        }

        var companyName = args[0];

        if (DeleteCustomCompany(companyName))
        {
            shell.WriteLine($"Successfully deleted custom company: {companyName}");
        }
        else
        {
            shell.WriteError($"Failed to delete company: {companyName}. It may not exist or be a preset company.");
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        // When a player connects, send them all existing custom companies
        if (args.NewStatus != SessionStatus.Connected || args.OldStatus == SessionStatus.Connected)
            return;

        _sawmill.Debug($"Sending {CustomCompanies.Count} custom companies to new player");

        // Send all existing custom companies to the new player
        foreach (var company in CustomCompanies.Values)
        {
            var netMsg = new CustomCompanyRegistryMessage(company);
            RaiseNetworkEvent(netMsg, args.Session);
            _sawmill.Debug($"Sent custom company to new player: {company.Name}");
        }
    }
}
