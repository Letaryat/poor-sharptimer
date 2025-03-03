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
    if (player.Team <= CsTeam.Spectator || player.ActionTrackingServices == null
      || player.IsBot)
      return;
    var slot = player.Slot;

    if (!playerTimers.TryGetValue(slot, out var timer)) return;

    if (timer.IsAddingStartZone || timer.IsAddingEndZone
      || timer.IsAddingBonusStartZone || timer.IsAddingBonusEndZone)
      return;

    if (timer is { IsTimerRunning: false, IsBonusTimerRunning: false }) return;

    var ticks = timer.TimerTicks;
    var span  = TimeSpan.FromSeconds(ticks / 64.0);

    var seconds = span.Seconds;
    var minutes = span.Minutes;

    player.ActionTrackingServices.MatchStats.Assists = seconds;
    player.ActionTrackingServices.MatchStats.Deaths  = minutes;

    if (!cachedPlacements.TryGetValue(slot, out var placement))
      fetchPlayerPlacement(slot, player.SteamID.ToString());
    else
      player.Score = -placement;

    Utilities.SetStateChanged(player, "CCSPlayerController",
      "m_pActionTrackingService");
  }

  private void fetchPlayerPlacement(int slot, string steamId) {
    Task.Run(async () => {
      var rank = (await GetPlayerServerRank(steamId)).Item1;
      cachedPlacements[slot] = rank;
    });
  }
}