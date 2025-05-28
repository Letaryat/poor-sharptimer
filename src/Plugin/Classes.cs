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

using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;

namespace SharpTimer
{
    // Cache for map ents
    public class EntityCache
    {
        public List<CBaseTrigger> Triggers { get; private set; }
        public List<CInfoTeleportDestination> InfoTeleportDestinations { get; private set; }
        public List<CPointEntity> InfoTargetEntities { get; private set; }

        public EntityCache()
        {
            Triggers = [];
            InfoTeleportDestinations = [];
            InfoTargetEntities = [];
            UpdateCache();
        }

        public void UpdateCache()
        {
            Triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple").ToList();
            InfoTeleportDestinations = Utilities.FindAllEntitiesByDesignerName<CInfoTeleportDestination>("info_teleport_destination").ToList();
        }
    }

    public class RecordCache
    {
        public Dictionary<int, PlayerRecord>? CachedWorldRecords { get; set; }
        public List<PlayerPoints>? CachedGlobalPoints { get; set; }
    }

    // MapData JSON
    public class MapInfo
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapStartTrigger { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapStartC1 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapStartC2 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BonusStartC1 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BonusStartC2 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapEndTrigger { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapEndC1 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapEndC2 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BonusEndC1 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BonusEndC2 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RespawnPos { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BonusRespawnPos { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OverrideDisableTelehop { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OverrideMaxSpeedLimit { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OverrideStageRequirement { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GlobalPointsMultiplier { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapTier { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MapType { get; set; }
    }

    public class PlayerTimerInfo
    {
        //timer
        public bool IsTimerRunning { get; set; }
        public int AFKTicks { get; set; }
        public bool AFKWarned { get; set; }
        public bool IsOnBhopBlock { get; set; }
        public bool IsNoclip { get; set; }
        public bool IsTimerBlocked { get; set; }
        public int TimerTicks { get; set; }
        public List<int> PrevTimerTicks { get; set; } = new();
        public int StageTicks { get; set; }
        public bool IsBonusTimerRunning { get; set; }
        public int BonusTimerTicks { get; set; }
        public int BonusStage { get; set; }
        public bool inStartzone { get; set; }
        public CurrentZoneInfo CurrentZoneInfo { get; set; } = new();
        public int currentStyle { get; set; }
        public bool changedStyle { get; set; }

        public CurrentMode Mode { get; set; }
        public enum CurrentMode
        {
            Classic,
            Arcade
        }

        //replay
        public bool IsReplaying { get; set; }
        public bool IsRecordingReplay { get; set; }

        //hud
        public string? ReplayHUDString { get; set; }
        public string? RankHUDIcon { get; set; }
        public string? CachedRank { get; set; }
        public bool IsRankPbCached { get; set; }
        public bool IsRankPbReallyCached { get; set; }
        public bool IsSpecTargetCached { get; set; }
        public string? PreSpeed { get; set; }
        public string? CachedPB { get; set; }
        public string? CachedMapPlacement { get; set; }
        public Dictionary<int, PlayerBonusPlacementInfo> CachedBonusInfo { get; set; } = new();

        //logic
        public int? TicksInAir { get; set; }
        public int TicksOnBhopBlock { get; set; }
        public int CheckpointIndex { get; set; }
        public Dictionary<int, int>? StageTimes { get; set; }
        public Dictionary<int, string>? StageVelos { get; set; }
        public int CurrentMapStage { get; set; }
        public int CurrentMapCheckpoint { get; set; }
        public CCSPlayer_MovementServices? MovementService { get; set; }
        public double Sync { get; set; }
        public int GoodSync { get; set; }
        public int TotalSync { get; set; }
        public List<QAngle_t> Rotation { get; set; } = new List<QAngle_t>();

        //player settings/stats
        public bool Azerty { get; set; }
        public bool HideTimerHud { get; set; }
        public bool HideKeys { get; set; }
        public bool HidePlayers { get; set; }
        public bool HideWeapon { get; set; }
        public bool GivenWeapon { get; set; }
        public bool SoundsEnabled { get; set; }
        public bool BindsDisabled { get; set; }
        public int PlayerFov { get; set; }
        public int TimesConnected { get; set; }
        public DateTime CmdCooldown { get; set; }
        public int TicksSinceLastRankUpdate { get; set; }

        //super special stuff for testers
        public bool IsTester { get; set; }
        public string? TesterSmolGif { get; set; }
        public string? TesterBigGif { get; set; }

        //vip stuff 
        public bool IsVip { get; set; }
        public string? VipReplayGif { get; set; }
        public string? VipBigGif { get; set; }

        //admin stuff
        public bool IsAddingStartZone { get; set; }
        public bool IsAddingBonusStartZone { get; set; }
        public string? StartZoneC1 { get; set; }
        public string? StartZoneC2 { get; set; }
        public string? BonusStartZoneC1 { get; set; }
        public string? BonusStartZoneC2 { get; set; }
        public bool IsAddingEndZone { get; set; }
        public bool IsAddingBonusEndZone { get; set; }
        public string? EndZoneC1 { get; set; }
        public string? EndZoneC2 { get; set; }
        public string? RespawnPos { get; set; }
        public string? BonusEndZoneC1 { get; set; }
        public string? BonusEndZoneC2 { get; set; }
        public string? BonusRespawnPos { get; set; }
        public Dictionary<int, CBeam>? ZoneToolWire { get; set; }

        //set respawn
        public string? SetRespawnPos { get; set; }
        public string? SetRespawnAng { get; set; }

        public class ViewAngle
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public ViewAngle (QAngle_t angles)
            {
                X = angles.X;
                Y = angles.Y;
                Z = angles.Z;
            }
        }

        public List<ViewAngle> ViewAngles { get; set; } = new List<ViewAngle>();
        public List<float> YawSpeed { get; set; } = new List<float>();
        public List<float> YawAccel { get; set; } = new List<float>();
        public List<double> AvgAccel { get; set; } = new List<double>();
        public double YawAccelPercent { get; set; }
        public List<double> YawAccelPercents { get; set; }  = new List<double>();
        public List<bool> MoveLeft { get; set; } = new List<bool>();
        public List<bool> MoveRight { get; set; } = new List<bool>();
        public int PerfectStrafes { get; set; }
        public int TotalStrafes { get; set; }
        public int MismatchedInputs { get; set; }
        public bool YawSpikeFlagged { get; set; } = false;
        public bool MismatchedInputsFlagged { get; set; } = false;
        public bool PerfectStrafesFlagged { get; set; } = false;
        public bool AHKFlagged { get; set; } = false;
    }

    public class CurrentZoneInfo
    {
        public bool InMainMapStartZone { get; set; }
        public bool InBonusStartZone { get; set; }
        public int CurrentBonusNumber { get; set; }
    }

    public class PlayerBonusPlacementInfo
    {
        public string? Placement { get; set; }

        public int PbTicks { get; set; }
    }

    //Replay stuff
    public class PlayerReplays
    {
        public int CurrentPlaybackFrame { get; set; }
        public int BonusX { get; set; }
        public int Style { get; set; }
        public List<ReplayFrames> replayFrames { get; set; } = [];
        public class ReplayFrames
        {
            public ReplayVector_t? Position { get; set; }
            public ReplayQAngle_t? Rotation { get; set; }
            public ReplayVector_t? Speed { get; set; }
            public PlayerButtons? Buttons { get; set; }
            public uint Flags { get; set; }
            public MoveType_t MoveType { get; set; }
        }
    }

    public class IndexedReplayFrames
    {
        public int Index { get; set; }
        public PlayerReplays.ReplayFrames? Frame { get; set; }
    }

    public class ReplayVector_t
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public ReplayVector_t(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static ReplayVector_t GetVector_tish(Vector_t actualVector_t)
        {
            return new ReplayVector_t(actualVector_t.X, actualVector_t.Y, actualVector_t.Z);
        }
        public static Vector_t ToVector_t(ReplayVector_t replayVector_t)
        {
            return new Vector_t(replayVector_t.X, replayVector_t.Y, replayVector_t.Z);
        }
    }

    public class ReplayQAngle_t
    {
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float Roll { get; set; }

        public ReplayQAngle_t(float pitch, float yaw, float roll)
        {
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
        }

        public static ReplayQAngle_t GetQAngle_tish(QAngle_t actualQAngle_t)
        {
            return new ReplayQAngle_t(actualQAngle_t.X, actualQAngle_t.Y, actualQAngle_t.Z);
        }

        public static QAngle_t ToQAngle_t(ReplayQAngle_t replayQAngle_t)
        {
            return new QAngle_t(replayQAngle_t.Pitch, replayQAngle_t.Yaw, replayQAngle_t.Roll);
        }
    }

    // PlayerRecords for JSON
    public class PlayerRecord
    {
        public int RecordID { get; set; }
        public string? PlayerName { get; set; }
        public string? SteamID { get; set; }
        public string? MapName { get; set; }
        public int TimerTicks { get; set; }
        public bool Replay { get; set; }
    }

    public class Record
    {
        public string? map_name { get; set; }
        public string? workshop_id { get; set; }
        public string? steamid { get; set; }
        public string? player_name { get; set; }
        public int timer_ticks { get; set; }
        public string? formatted_time { get; set; }
        public long unix_stamp { get; set; }
        public int times_finished { get; set; }
        public int style { get; set; }
        public int points { get; set; }
        public int max_velocity { get; set; }
        public float air_max_wishspeed { get; set; }
        public string? hostname { get; set; }
        public string? ip { get; set; }
        public string? hash { get; set; }
    }

    public class ReplayData
    {
        public int record_id { get; set; }
        public string? map_name { get; set; }
        public int style { get; set; }
        public string? hash { get; set; }
        public string? replay_data { get; set; }
    }

    // PlayerPoints for MySql
    public class PlayerPoints
    {
        public string? PlayerName { get; set; }
        public int GlobalPoints { get; set; }
    }

    // KZ checkpoints
    public class PlayerCheckpoint
    {
        public string? PositionString { get; set; }
        public string? RotationString { get; set; }
        public string? SpeedString { get; set; }
    }

    // Stage times and velos
    public class PlayerStageData
    {
        public Dictionary<int, int>? StageTimes { get; set; }
        public Dictionary<int, string>? StageVelos { get; set; }
    }
}