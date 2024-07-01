/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void DamageHook()
        {
            try
            {
                SharpTimerDebug("Init Damage hook...");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    SharpTimerDebug("Trying to register Linux Damage hook...");
                    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(this.OnTakeDamage, HookMode.Pre);
                }
                else
                {
                    SharpTimerDebug("Trying to register Windows Damage hook...");
                    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid function pointer")
                    SharpTimerError($"Error in DamageHook: Conflict between cs2fixes and SharpTimer");
                else
                    SharpTimerError($"Error in DamageHook: {ex.Message}");
            }
        }

        public void DamageUnHook()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid function pointer")
                    SharpTimerError($"Error in DamageUnHook: Conflict between cs2fixes and SharpTimer");
                else
                    SharpTimerError($"Error in DamageUnHook: {ex.Message}");
            }
        }

        HookResult OnTakeDamage(DynamicHook h)
        {
            var ent = h.GetParam<CEntityInstance>(0);
            var info = h.GetParam<CTakeDamageInfo>(1);
            if(disableDamage) h.GetParam<CTakeDamageInfo>(1).Damage = 0;

            if (!ent.IsValid || !info.Attacker.IsValid)
                return HookResult.Continue;

            if (ent.DesignerName == "player" && info.Attacker.Value!.DesignerName == "player")
                return HookResult.Handled;
            else
                return HookResult.Continue;
        }

        HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (disableDamage == true)
            {
                var player = @event.Userid;

                if (!player!.IsValid)
                    return HookResult.Continue;

                Vector playerSpeed = player!.PlayerPawn.Value!.AbsVelocity ?? new Vector(0, 0, 0);

                player.PlayerPawn.Value.Health = int.MaxValue;
                player.PlayerPawn.Value.ArmorValue = int.MaxValue;

                if (!player.PawnHasHelmet)
                    player.GiveNamedItem("item_assaultsuit");

                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player)) AdjustPlayerVelocity(player, playerSpeed.Length(), true);
                });
            }

            return HookResult.Continue;
        }
    }
}