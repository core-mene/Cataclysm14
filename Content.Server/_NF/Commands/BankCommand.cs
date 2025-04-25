using System.Linq;
using Content.Server.Administration;
using Content.Server.Preferences.Managers;
using Content.Server._NF.Bank;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Content.Shared._NF.Bank.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._NF.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class BankCommand : IConsoleCommand
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "bank";

    public string Description => "Modifies a player's bank account balance.";

    public string Help => "bank <username/id> <amount>\n" +
                          "Adds or removes the specified amount from the player's bank account. " +
                          "Use positive values to add money, negative values to remove money.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Wrong number of arguments.\nUsage: " + Help);
            return;
        }

        var target = args[0];
        var bankSystem = _entitySystemManager.GetEntitySystem<BankSystem>();

        if (!int.TryParse(args[1], out var amount))
        {
            shell.WriteError("Amount must be a valid number.");
            return;
        }

        // Try to find player by name first
        ICommonSession? targetSession = null;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Name.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                targetSession = session;
                break;
            }
        }

        // If player name not found, try by ID
        if (targetSession == null && Guid.TryParse(target, out var userId))
        {
            targetSession = _playerManager.Sessions.FirstOrDefault(s => s.UserId == userId);
        }

        if (targetSession == null)
        {
            shell.WriteError($"Unable to find player '{target}'.");
            return;
        }

        if (!_prefsManager.TryGetCachedPreferences(targetSession.UserId, out var prefs))
        {
            shell.WriteError($"Unable to retrieve preferences for player '{target}'.");
            return;
        }

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            shell.WriteError($"Player '{target}' has an invalid character profile.");
            return;
        }

        var currentBalance = profile.BankBalance;

        // Ensure the player won't have negative balance after withdrawal
        if (amount < 0 && Math.Abs(amount) > currentBalance)
        {
            shell.WriteError($"Player '{target}' only has {currentBalance}, cannot remove {Math.Abs(amount)}.");
            return;
        }

        bool success;
        int? newBalance = null;

        // Check if player is currently in-game with an entity
        EntityUid? playerEntity = targetSession.AttachedEntity;

        if (playerEntity != null && _entityManager.HasComponent<BankAccountComponent>(playerEntity.Value))
        {
            // Player is in-game with entity that has bank account - use entity methods which will update the profile
            if (amount > 0)
            {
                success = bankSystem.TryBankDeposit(playerEntity.Value, amount);
                if (success)
                {
                    // Get updated balance after deposit
                    success = bankSystem.TryGetBalance(targetSession, out int updatedBalance);
                    if (success)
                        newBalance = updatedBalance;
                }
            }
            else if (amount < 0)
            {
                success = bankSystem.TryBankWithdraw(playerEntity.Value, Math.Abs(amount));
                if (success)
                {
                    // Get updated balance after withdrawal
                    success = bankSystem.TryGetBalance(targetSession, out int updatedBalance);
                    if (success)
                        newBalance = updatedBalance;
                }
            }
            else
            {
                shell.WriteLine($"Player '{target}' balance unchanged: {currentBalance}");
                return;
            }

            if (success)
                shell.WriteLine("Updated player's bank account.");
        }
        else
        {
            // Player is not in-game or entity has no bank account - update profile directly
            if (amount > 0)
            {
                success = bankSystem.TryBankDeposit(targetSession, prefs, profile, amount, out newBalance);
            }
            else if (amount < 0)
            {
                success = bankSystem.TryBankWithdraw(targetSession, prefs, profile, Math.Abs(amount), out newBalance);
            }
            else
            {
                shell.WriteLine($"Player '{target}' balance unchanged: {currentBalance}");
                return;
            }
        }

        if (!success || newBalance == null)
        {
            shell.WriteError($"Failed to modify player '{target}' bank balance.");
            return;
        }

        shell.WriteLine(amount > 0
            ? $"Added {amount} to player '{target}' balance. New balance: {newBalance.Value}"
            : $"Removed {Math.Abs(amount)} from player '{target}' balance. New balance: {newBalance.Value}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new List<CompletionOption>();

            // Add all online players as completion options
            foreach (var session in IoCManager.Resolve<IPlayerManager>().Sessions)
            {
                options.Add(new CompletionOption(session.Name, $"Player: {session.Name}"));
            }

            return CompletionResult.FromOptions(options);
        }

        if (args.Length == 2)
        {
            // For the amount parameter, provide some common values as suggestions
            var amountOptions = new List<CompletionOption>
            {
                new CompletionOption("100", "Add 100 credits"),
                new CompletionOption("1000", "Add 1000 credits"),
                new CompletionOption("10000", "Add 10000 credits"),
                new CompletionOption("-100", "Remove 100 credits"),
                new CompletionOption("-1000", "Remove 1000 credits")
            };

            return CompletionResult.FromOptions(amountOptions);
        }

        return CompletionResult.Empty;
    }
}
