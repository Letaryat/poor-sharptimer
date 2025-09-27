using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace SharpTimer;

public partial class SharpTimer
{
    private readonly ConVar? _wishspeed = ConVar.Find("sv_air_max_wishspeed");
    private readonly ConVar? _airaccel = ConVar.Find("sv_airaccelerate");
    private readonly ConVar? _accel = ConVar.Find("sv_accelerate");
    private readonly ConVar? _friction = ConVar.Find("sv_friction");
    
    public readonly struct ModeConfig
    {
        public readonly Mode Mode;
        public readonly float AirAccelerate;
        public readonly float Accelerate;
        public readonly float Wishspeed;
        public readonly float Friction;

        public ModeConfig(Mode mode, float airAccelerate, float accelerate, float wishspeed, float friction)
        {
            Mode = mode;
            AirAccelerate = airAccelerate;
            Accelerate = accelerate;
            Wishspeed = wishspeed;
            Friction = friction;
        }
    }

    private ModeConfig[] _configValues;

    public void InitializeModeConfigs()
    {
        _configValues = new ModeConfig[]
        {
            new(Mode.Standard, 150f, 10f, 30.0f, 5.2f),
            new(Mode._85t, 150f, 10f, 37.41f, 5.2f),
            new(Mode._102t, 150f, 10f, 43.55f, 5.2f),
            new(Mode._128t, 150f, 10f, 52.59f, 5.2f),
            new(Mode.Source, 150f, 5f, 30.71f, 4f),
            new(Mode.Bhop, 1000f, 10f, 52.59f, 5.2f),
            new(Mode.Custom, customAirAccel, customAccel, customWishSpeed, customFriction)
        };
    }

    private readonly Dictionary<Mode, int> ModeIndexLookup = new()
    {
        { Mode.Standard, 0 },
        { Mode._85t, 1 },
        { Mode._102t, 2 },
        { Mode._128t, 3 },
        { Mode.Source, 4 },
        { Mode.Bhop, 5 },
        { Mode.Custom, 6 }
    };

    private bool TryParseMode(string input, out Mode mode)
    {
        if (Enum.TryParse<Mode>(input, true, out mode) && ModeManager.IsModeEnabled(mode))
        {
            return true;
        }

        var modes = ModeManager.GetEnabledModes();
        foreach (var m in modes)
        {
            if (GetModeName(m).Equals(input, StringComparison.OrdinalIgnoreCase))
            {
                mode = m;
                return true;
            }
        }

        mode = default;
        return false;
    }

    public void SetPlayerMode(CCSPlayerController player, Mode mode)
    {
        playerTimers[player.Slot].Mode = GetModeName(mode);
        playerTimers[player.Slot].ChangedMode = true;
        
        Server.NextFrame(async () =>
        {
            await SetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, player.Slot);
        });
        playerTimers[player.Slot].RespawnPos = "";
        playerTimers[player.Slot].BonusRespawnPos = "";
        ApplyModeSettings(player, mode);
    }

    private void ApplyModeSettings(CCSPlayerController player, Mode mode)
    {
        var config = _configValues[ModeIndexLookup[mode]];

        player.ReplicateConVar("sv_airaccelerate", config.AirAccelerate.ToString(CultureInfo.InvariantCulture));
        player.ReplicateConVar("sv_accelerate", config.Accelerate.ToString(CultureInfo.InvariantCulture));
        player.ReplicateConVar("sv_air_max_wishspeed", config.Wishspeed.ToString(CultureInfo.InvariantCulture));
        player.ReplicateConVar("sv_friction", config.Friction.ToString(CultureInfo.InvariantCulture));
    }

    private string GetModeName(Mode mode)
    {
        return mode switch
        {
            Mode.Standard => "Standard",
            Mode._85t => "85t",
            Mode._102t => "102t",
            Mode._128t => "128t",
            Mode.Source => "Source",
            Mode.Bhop => "Bhop",
            Mode.Custom => "Custom",
            _ => mode.ToString()
        };
    }

    public double GetModeMultiplier(string mode, bool global = false)
    {
        if (global)
            return 1;

        switch (mode.ToLower())
        {
            case "source":
                return sourceModeModifier;
            case "standard":
                return standardModeModifier;
            case "85t":
                return _85tModeModifier;
            case "102t":
                return _102tModeModifier;
            case "128t":
                return _128tModeModifier;
            case "bhop":
                return bhopModeModifier;
            case "custom":
                return 1;
            default:
                return 1;
        }
    }

    [ConsoleCommand("css_mode")]
    public void ModeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAllowedPlayer(player))
            return;

        if (!player!.PlayerPawn.Value!.AbsVelocity.IsZero())
        {
            Utils.PrintToChat(player, Localizer["modes_moving"]);
            return;
        }

        if (CommandCooldown(player))
            return;

        var desiredMode = command.GetArg(1);

        playerTimers[player.Slot].IsTimerRunning = false;
        playerTimers[player.Slot].TimerTicks = 0;
        playerTimers[player.Slot].IsBonusTimerRunning = false;
        playerTimers[player.Slot].BonusTimerTicks = 0;

        if (command.ArgByIndex(1) == "")
        {
            var modes = ModeManager.GetEnabledModes().ToArray();

            for (int i = 0; i < modes.Length; i++)
            {
                Utils.PrintToChat(player, Localizer["modes_list", i, GetModeName(modes[i])]);
            }

            Utils.PrintToChat(player, Localizer["mode_example"]);
            return;
        }

        if (TryParseMode(desiredMode.ToLower(), out Mode newMode))
        {
            SetPlayerMode(player, newMode);
            Utils.PrintToChat(player, Localizer["mode_set", GetModeName(newMode)]);
            AddTimer(0.1f, () =>
            {
                RespawnPlayer(player);
            });
        }
        else
        {
            Utils.PrintToChat(player, Localizer["mode_not_found", desiredMode]);
        }
    }

    private void ApplyModeCvars(CCSPlayerController player)
    {
        if (player == null || player.IsBot || !player.IsValid || player.IsHLTV) return;

        if (!TryParseMode(playerTimers[player.Slot].Mode, out Mode playerMode)) return;
        
        if (_accel == null || _airaccel == null || _wishspeed == null || _friction == null)
        {
            Utils.LogDebug("ApplyConvar: Mode convar values are null");
            return;
        }

        ModeConfig modeConfig = _configValues[ModeIndexLookup[playerMode]];
        
        _accel.SetValue(modeConfig.Accelerate);
        _airaccel.SetValue(modeConfig.AirAccelerate);
        _wishspeed.SetValue(modeConfig.Wishspeed);
        _friction.SetValue(modeConfig.Friction);
    }
}

[Flags]
public enum Mode
{
    Standard = 0,  // default csgo 1:1 cfg (64 tick)
    _85t = 1,      // 85t-ish speed (37.41 wishspeed)
    _102t = 2,     // 102.4t-ish speed (43.55 wishspeed)
    _128t = 3,     // 128t-ish speed (52.59 wishspeed)
    Source = 4,    // 66t-ish + lower accel + cs:s friction (30.71 wishspeed & 5 accel & 4 friction)
    Bhop = 5,      // 128t-ish + 1000 aa (52.59 wishspeed & 1000 aa)
    Custom = 6     // Use custom server cvars (WILL NOT SUBMIT TO GLOBAL)
}


public class ModeManager
{
    private static HashSet<Mode> enabledModes =
        [Mode.Standard, Mode._85t, Mode._102t, Mode._128t, Mode.Source, Mode.Bhop, Mode.Custom];

    public static bool IsModeEnabled(Mode mode) => enabledModes.Contains(mode);
    
    public static void EnableMode(Mode mode) => enabledModes.Add(mode);
    
    public static void DisableMode(Mode mode) => enabledModes.Remove(mode);
    
    public static IEnumerable<Mode> GetEnabledModes() => enabledModes.ToList();
}