// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server._Mono.Administration.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.Host)]
public sealed class ToggleAdminLoggingCommand : LocalizedCommands
{
    public override string Command => "toggleadminlogging";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("cmd-toggleadminlogging-no-console"));
            return;
        }

        var mgr = IoCManager.Resolve<IAdminManager>();

        var adminData = mgr.GetAdminData(player);

        DebugTools.AssertNotNull(adminData);

        if (!adminData!.LoggingDisabled)
        {
            mgr.DisableLogging(player);
        }
        else
        {
            mgr.EnableLogging(player);
        }
    }
}
