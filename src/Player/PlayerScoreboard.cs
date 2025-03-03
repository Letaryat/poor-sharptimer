using System.Net;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer;

public partial class SharpTimer {
  private Dictionary<int, int> cachedPlacements = new();

  public void AssignPlayerScoreboards() {
    foreach (var player in connectedPlayers.Values.Where(player
      => player.IsValid))
      assignScoreboard(player);
  }

  private void assignScoreboard(CCSPlayerController player) {
    if (player.Team <= CsTeam.Spectator || player.IsBot) return;
    var matchStats = player.ActionTrackingServices?.MatchStats;
    if (matchStats == null) return;

    var slot = player.Slot;

    if (!playerTimers.TryGetValue(slot, out var timer)) return;

    if (timer.IsAddingStartZone || timer.IsAddingEndZone
      || timer.IsAddingBonusStartZone || timer.IsAddingBonusEndZone)
      return;

    var ticks = timer.TimerTicks;
    var span  = TimeSpan.FromSeconds(ticks / 64.0);

    var seconds = span.Seconds;
    var minutes = span.Minutes;

    matchStats.Assists = seconds;
    matchStats.Deaths  = minutes;

    if (!cachedPlacements.TryGetValue(slot, out var placement))
      fetchPlayerPlacement(slot, player.SteamID.ToString());
    else
      player.Score = -placement;

    if (stageTriggerCount == 1) { // Linear map, show checkpoints
      matchStats.Kills = timer.CurrentMapCheckpoint;
    } else {
      matchStats.Kills = timer.IsBonusTimerRunning ?
        -timer.BonusStage :
        timer.CurrentMapStage;
    }

    Utilities.SetStateChanged(player, "CCSPlayerController",
      "m_pActionTrackingServices");
  }

  private void fetchPlayerPlacement(int slot, string steamId) {
    Task.Run(async () => {
      var rank = (await GetPlayerServerRank(steamId)).Item1;
      cachedPlacements[slot] = rank;
    });
  }
}