using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SharpTimer;

public static class EntityExtends
{
    public static bool Valid(this CCSPlayerController? player)
    {
        if (player == null) return false;

        return player.IsValid && !player.IsBot && !player.IsHLTV;
    }

    public static CCSPlayerPawn? PlayerPawn(this CCSPlayerController? player)
    {
        if (player == null) return null;

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        return playerPawn;
    }

    public static CBasePlayerPawn? Pawn(this CCSPlayerController? player)
    {
        if (player == null) return null;

        CBasePlayerPawn? pawn = player.Pawn.Value;

        return pawn;
    }

    public static bool TeamT([NotNullWhen(true)] this CCSPlayerController player)
    {
        return player.Team == CsTeam.Terrorist;
    }
    public static bool TeamCT([NotNullWhen(true)] this CCSPlayerController player)
    {
        return player.Team == CsTeam.CounterTerrorist;
    }
    public static bool TeamSpec([NotNullWhen(true)] this CCSPlayerController player)
    {
        return player.Team == CsTeam.Spectator;
    }
    public static bool TeamNone([NotNullWhen(true)] this CCSPlayerController player)
    {
        return player.Team == CsTeam.None;
    }
}