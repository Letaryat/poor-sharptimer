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

using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        //Because decimals are weird
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        
        [ConsoleCommand("sharptimer_hostname", "Default Server Hostname.")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerServerHostname(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                defaultServerHostname = $"A SharpTimer Server";
                return;
            }

            defaultServerHostname = $"{args}";
        }

        [ConsoleCommand("sharptimer_autoset_mapinfo_hostname_enabled", "Whether Map Name and Map Tier (if available) should be put into the hostname or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHostnameConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            autosetHostname = bool.TryParse(args, out bool autosetHostnameValue) ? autosetHostnameValue : args != "0" && autosetHostname;
        }

        [ConsoleCommand("sharptimer_custom_map_cfgs_enabled", "Whether Custom Map .cfg files should be executed for the corresponding maps (found in cfg/SharpTimer/MapData/MapExecs/kz_example.cfg). Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCustomMapExecConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            execCustomMapCFG = bool.TryParse(args, out bool execCustomMapCFGValue) ? execCustomMapCFGValue : args != "0" && execCustomMapCFG;
        }

        [ConsoleCommand("sharptimer_display_rank_tags_chat", "Whether the plugin should display rank tags infront of players names in chat or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerChatRankTagsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            displayChatTags = bool.TryParse(args, out bool displayChatTagsValue) ? displayChatTagsValue : args != "0" && displayChatTags;
        }

        [ConsoleCommand("sharptimer_display_rank_tags_scoreboard", "Whether the plugin should display rank tags infront of players names in scoreboard or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerScoreboardRankTagsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            displayScoreboardTags = bool.TryParse(args, out bool displayScoreboardTagsValue) ? displayScoreboardTagsValue : args != "0" && displayScoreboardTags;
        }

        [ConsoleCommand("sharptimer_global_rank_points_enabled", "Whether the plugin should reward players with global points for completing maps. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            globalRanksEnabled = bool.TryParse(args, out bool globalRanksEnabledValue) ? globalRanksEnabledValue : args != "0" && globalRanksEnabled;
        }

        [ConsoleCommand("sharptimer_global_rank_min_points_threshold", "Players with Points below this amount will be treated as Unranked. Default value: 1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksMinPointsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int minPoints) && minPoints > 0)
            {
                minGlobalPointsForRank = minPoints;
                SharpTimerConPrint($"SharpTimer min points for rank set to {minPoints} points.");
            }
            else
            {
                SharpTimerConPrint("Invalid min points for rank value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_bonus_points_multiplier", "Multiplier for bonus course completion points. Default value: 0.5")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBonusPointsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                globalPointsBonusMultiplier = multiplier;
                SharpTimerConPrint($"SharpTimer bonus points multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid bonus points multiplier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t1", "Baseline points awarded for completing a T1 map. Default value: 25")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT1Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT1 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T1 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T1 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t2", "Baseline points awarded for completing a T2 map. Default value: 50")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT2Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT2 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T2 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T2 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t3", "Baseline points awarded for completing a T3 map. Default value: 100")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT3Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT3 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T3 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T3 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t4", "Baseline points awarded for completing a T4 map. Default value: 200")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT4Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT4 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T4 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T4 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t5", "Baseline points awarded for completing a T5 map. Default value: 400")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT5Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT5 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T5 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T5 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t6", "Baseline points awarded for completing a T6 map. Default value: 600")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT6Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT6 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T6 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T6 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t7", "Baseline points awarded for completing a T7 map. Default value: 800")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT7Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT7 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T7 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T7 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_baseline_t8", "Baseline points awarded for completing a T8 map. Default value: 1000")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksBaselineT8Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                baselineT8 = baseline;
                SharpTimerConPrint($"SharpTimer baseline T8 points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid baseline T8 points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_max_record_points", "Baseline max points for records, multiplied by tiernum. Default value: 250")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksMaxRecordPointsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int baseline) && baseline > 0)
            {
                maxRecordPointsBase = baseline;
                SharpTimerConPrint($"SharpTimer max record points set to {baseline}");
            }
            else
            {
                SharpTimerConPrint("Invalid max record points. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_max_completions", "Maximum completions allowed to receive points (0 = infinite). Default value: 0")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksMaxCompletionsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int completions) && completions >= 0)
            {
                globalPointsMaxCompletions = completions;
                SharpTimerConPrint($"SharpTimer max completions set to {completions}");
            }
            else
            {
                SharpTimerConPrint("Invalid max completions. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_1", "Max points multiplier for top 10, #1. Default value: 1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop101Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_1 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_1 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_1 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_2", "Max points multiplier for top 10, #2. Default value: 0.8")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop102Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_2 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_2 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_2 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_3", "Max points multiplier for top 10, #3. Default value: 0.75")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop103Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_3 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_3 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_3 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_4", "Max points multiplier for top 10, #4. Default value: 0.7")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop104Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_4 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_4 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_4 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_5", "Max points multiplier for top 10, #5. Default value: 0.65")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop105Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_5 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_5 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_5 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_6", "Max points multiplier for top 10, #6. Default value: 0.6")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop106Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_6 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_6 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_6 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_7", "Max points multiplier for top 10, #7. Default value: 0.55")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop107Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_7 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_7 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_7 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_8", "Max points multiplier for top 10, #8. Default value: 0.5")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop108Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_8 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_8 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_8 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_9", "Max points multiplier for top 10, #9. Default value: 0.45")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop109Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_9 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_9 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_9 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_top10_10", "Max points multiplier for top 10, #10. Default value: 0.4")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksTop1010Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double multiplier) && multiplier is >= 0 and <= 1)
            {
                top10_10 = multiplier;
                SharpTimerConPrint($"SharpTimer top10_10 multiplier set to {multiplier}");
            }
            else
            {
                SharpTimerConPrint("Invalid top10_10 multiplier. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_group1_percentile", "The top % of record holders in group #1. Default value: 3.125")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksGroup1Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double percentile) && percentile is >= 0 and <= 100)
            {
                group1 = percentile;
                SharpTimerConPrint($"SharpTimer group #1 percentile set to {percentile}");
            }
            else
            {
                SharpTimerConPrint("Invalid group #1 percentile. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_group2_percentile", "The top % of record holders in group #2. Default value: 6.25")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksGroup2Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double percentile) && percentile is >= 0 and <= 100)
            {
                group2 = percentile;
                SharpTimerConPrint($"SharpTimer group #2 percentile set to {percentile}");
            }
            else
            {
                SharpTimerConPrint("Invalid group #2 percentile. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_group3_percentile", "The top % of record holders in group #3. Default value: 12.5")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksGroup3Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double percentile) && percentile is >= 0 and <= 100)
            {
                group3 = percentile;
                SharpTimerConPrint($"SharpTimer group #3 percentile set to {percentile}");
            }
            else
            {
                SharpTimerConPrint("Invalid group #3 percentile. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_group4_percentile", "The top % of record holders in group #4. Default value: 25")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksGroup4Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double percentile) && percentile is >= 0 and <= 100)
            {
                group4 = percentile;
                SharpTimerConPrint($"SharpTimer group #4 percentile set to {percentile}");
            }
            else
            {
                SharpTimerConPrint("Invalid group #4 percentile. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_global_rank_group5_percentile", "The top % of record holders in group #5. Default value: 50")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksGroup5Convar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double percentile) && percentile is >= 0 and <= 100)
            {
                group5 = percentile;
                SharpTimerConPrint($"SharpTimer group #5 percentile set to {percentile}");
            }
            else
            {
                SharpTimerConPrint("Invalid group #5 percentile. Please provide a positive double.");
            }
        }

        [ConsoleCommand("sharptimer_replays_enabled", "Whether replays should be enabled or not. This option might be performance taxing and use more ram & cpu. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableReplays = bool.TryParse(args, out bool enableReplaysValue) ? enableReplaysValue : args != "0" && enableReplays;
        }
        
        [ConsoleCommand("sharptimer_replay_only_sr", "Only saves SR replay if true. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplaySRConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            onlySRReplay = bool.TryParse(args, out bool value) ? value : args != "0" && onlySRReplay;
        }

        [ConsoleCommand("sharptimer_replay_max_length", "The maximum length for a Replay to be saved in seconds. Anything longer will be discarded Default value: 300")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayMaxLengthConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int mxLength) && mxLength > 0)
            {
                maxReplayFrames = (int)(mxLength * 64);
                SharpTimerConPrint($"SharpTimer max replay length set to {mxLength} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid max replay length value. Please provide a positive int.");
            }
        }

        [ConsoleCommand("sharptimer_replay_bot_enabled", "Whether a looping Server Record bot should be spawned in or not (requires navmesh fix). Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayBotConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableSRreplayBot = bool.TryParse(args, out bool enableSRreplayBotValue) ? enableSRreplayBotValue : args != "0" && enableSRreplayBot;
        }

        [ConsoleCommand("sharptimer_replay_bot_name", "What the name of the Replay Record bot should be. Default value: SERVER RECORD REPLAY")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayBotNameConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                replayBotName = $"SERVER RECORD REPLAY";
                return;
            }

            replayBotName = $"{args}";
        }

        /*[ConsoleCommand("sharptimer_vip_gif_host", "URL where VIP gifs are being hosted on. Default: 'https://files.catbox.moe'")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerVipGifHost(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                vipGifHost = $"https://files.catbox.moe";
                return;
            }

            vipGifHost = $"{args}";
        }*/

        [ConsoleCommand("sharptimer_jumpstats_enabled", "Whether JumpStats are enabled or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerJumpStatsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            jumpStatsEnabled = bool.TryParse(args, out bool jumpStatsEnabledValue) ? jumpStatsEnabledValue : args != "0" && jumpStatsEnabled;
        }

        [ConsoleCommand("sharptimer_jumpstats_min_distance", "Defines the minimum distance for a jumpstat to be printed to chat. Default value: 175.0")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerJumpStatsMinDistConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float dist) && dist > 0)
            {
                jumpStatsMinDist = dist;
                SharpTimerConPrint($"SharpTimer JumpStats min distance set to {dist} units.");
            }
            else
            {
                SharpTimerConPrint("Invalid JumpStats min distance value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_jumpstats_max_vert", "Defines the max vertical distance for a jumpstat to not be printed to chat. Default value: 32.0")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerJumpStatsMaxVertConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float dist) && dist > 0)
            {
                jumpStatsMaxVert = dist;
                SharpTimerConPrint($"SharpTimer JumpStats max vert distance set to {dist} units.");
            }
            else
            {
                SharpTimerConPrint("Invalid JumpStats max vert distance value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_jumpstats_movement_unlocker_cap", "Intended for taming movement unlocker, caps speed on the second tick of a player being on the ground. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerJumpStatsUnlockerCapConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            movementUnlockerCapEnabled = bool.TryParse(args, out bool movementUnlockerCapEnabledValue) ? movementUnlockerCapEnabledValue : args != "0" && movementUnlockerCapEnabled;
        }

        [ConsoleCommand("sharptimer_jumpstats_movement_unlocker_cap_value", "Speed cap value which will kick in on the second tick of the player being on the ground. Default value: 250.0")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerJumpStatsUnlockerCapValueConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float value) && value > 0)
            {
                movementUnlockerCapValue = value;
                if (movementUnlockerCapEnabled) SharpTimerConPrint($"SharpTimer JumpStats Movement Unlocker cap value set to {value} units.");
            }
            else
            {
                SharpTimerConPrint("Invalid JumpStats Movement Unlocker cap value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_global_cache_interval", "Refresh interval of record and point caching. Default value: 120")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalCacheConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int value) && value > 0)
            {
                globalCacheInterval = value;
                SharpTimerConPrint($"SharpTimer global cache refresh interval set to {value} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid global ache refresh interval value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_kill_pointservercommand_entities", "If True the plugin will kill all point_servercommand ents (necessary to make xplay maps usable due to them being bad ports). Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPointServerCommandConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            killServerCommands = bool.TryParse(args, out bool killServerCommandsValue) ? killServerCommandsValue : args != "0" && killServerCommands;
        }
        [ConsoleCommand("sharptimer_enable_anticheat", "If enabled, suspicious players will be flagged. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAnticheatConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            useAnticheat = bool.TryParse(args, out bool value) ? value : args != "0" && useAnticheat;
        }

        [ConsoleCommand("sharptimer_enable_afk_hibernation", "If True the plugin will reduce functionality to afk players to improve performance. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAFKHibernationConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            afkHibernation = bool.TryParse(args, out bool afkValue) ? afkValue : args != "0" && afkHibernation;
        }

        [ConsoleCommand("sharptimer_enable_afk_warning", "If true the plugin will warn the player who is about to be moved to afk. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAFKWarningConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            afkWarning = bool.TryParse(args, out bool afkValue) ? afkValue : args != "0" && afkWarning;
        }

        [ConsoleCommand("sharptimer_afk_seconds", "Amount of seconds a player is allowed to afk before being hibernated. Default value: 60")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAFKSecondsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int secs) && secs >= 0)
            {
                afkSeconds = secs;
                SharpTimerConPrint($"SharpTimer afk period: {secs}s");
            }
            else
            {
                SharpTimerConPrint("Invalid afk period. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_hud_updates_per_second", "Updates per second of the HUD. Default value: 64")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHUDUpdatesConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int tickrate) && tickrate >= 0 && tickrate <= 64)
            {
                hudTickrate = tickrate;
                SharpTimerConPrint($"SharpTimer hud updates per second: {tickrate}");
            }
            else
            {
                SharpTimerConPrint("Invalid HUD updates per second. Please provide a positive integer below 64.");
            }
        }

        [ConsoleCommand("sharptimer_enable_timer_hud", "If Timer Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            hudOverlayEnabled = bool.TryParse(args, out bool hudOverlayEnabledValue) ? hudOverlayEnabledValue : args != "0" && hudOverlayEnabled;
        }

        [ConsoleCommand("sharptimer_enable_keys_hud", "If Keys Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPointKeysHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            keysOverlayEnabled = bool.TryParse(args, out bool keysOverlayEnabledValue) ? keysOverlayEnabledValue : args != "0" && keysOverlayEnabled;
        }

        [ConsoleCommand("sharptimer_enable_rankicons_hud", "If Rank Icons Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPointRankIconsHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            RankIconsEnabled = bool.TryParse(args, out bool rankIconsEnabledValue) ? rankIconsEnabledValue : args != "0" && RankIconsEnabled;
        }

        [ConsoleCommand("sharptimer_enable_velocity_hud", "If Speed Velocity Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerVelocityHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            VelocityHudEnabled = bool.TryParse(args, out bool VelocityHudValue) ? VelocityHudValue : args != "0" && VelocityHudEnabled;
        }

        [ConsoleCommand("sharptimer_enable_strafesync_hud", "If Stafe Sync % Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerStrafeHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            StrafeHudEnabled = bool.TryParse(args, out bool StrafeHudValue) ? StrafeHudValue : args != "0" && StrafeHudEnabled;
        }

        [ConsoleCommand("sharptimer_enable_map_tier_hud", "If Map Tier Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMapTierHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            MapTierHudEnabled = bool.TryParse(args, out bool MapTierHudValue) ? MapTierHudValue : args != "0" && MapTierHudEnabled;
        }

        [ConsoleCommand("sharptimer_enable_map_type_hud", "If Map Type Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMapTypeHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            MapTypeHudEnabled = bool.TryParse(args, out bool MapTypeHudValue) ? MapTypeHudValue : args != "0" && MapTypeHudEnabled;
        }

        [ConsoleCommand("sharptimer_enable_map_name_hud", "If Map Name Hud should be globally enabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMapNameHUDConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            MapNameHudEnabled = bool.TryParse(args, out bool MapNameHudValue) ? MapNameHudValue : args != "0" && MapNameHudEnabled;
        }

        [ConsoleCommand("sharptimer_debug_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConPrintConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableDebug = bool.TryParse(args, out bool enableDebugValue) ? enableDebugValue : args != "0" && enableDebug;
        }

        [ConsoleCommand("sharptimer_remote_data_disable", "Disable fetching map tier and type from github. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            disableRemoteData = bool.TryParse(args, out bool value) ? value : args != "0" && disableRemoteData;
        }

        [ConsoleCommand("sharptimer_global_api_key", "Global api key")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAPIKeyConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                apiKey = $"";
                return;
            }

            apiKey = $"{args}";
        }

        [ConsoleCommand("sharptimer_enable_checkpoint_verification", "Enable or disable checkpoint verification system. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCheckpointVerificationConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            useCheckpointVerification = bool.TryParse(args, out bool value) ? value : args != "0" && useCheckpointVerification;
        }
        
        [ConsoleCommand("sharptimer_apply_infinite_ammo", "Enable or disable infinite ammo. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerInfiniteAmmoConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            applyInfiniteAmmo = bool.TryParse(args, out bool value) ? value : args != "0" && applyInfiniteAmmo;
        }

        [ConsoleCommand("sharptimer_use2Dspeed_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimer2dSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            use2DSpeed = bool.TryParse(args, out bool use2DSpeedValue) ? use2DSpeedValue : args != "0" && use2DSpeed;
        }

        [ConsoleCommand("sharptimer_override_beam_colors_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerBeamColorsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            beamColorOverride = bool.TryParse(args, out bool beamColorOverrideValue) ? beamColorOverrideValue : args != "0" && beamColorOverride;
        }

        [ConsoleCommand("sharptimer_start_beam_color", "Start beam color, Requires sharptimer_override_beam_colors_enabled true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerStartBeamColor(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                startBeamColor = $"";
                return;
            }

            startBeamColor = $"{args}";
        }

        [ConsoleCommand("sharptimer_end_beam_color", "Start beam color, Requires sharptimer_override_beam_colors_enabled true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerEndBeamColor(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                endBeamColor = $"";
                return;
            }

            endBeamColor = $"{args}";
        }

        [ConsoleCommand("sharptimer_mysql_enabled", "Whether player times should be put into a mysql database by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMySQLConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            useMySQL = bool.TryParse(args, out bool useMySQLValue) ? useMySQLValue : args != "0" && useMySQL;
        }

        [ConsoleCommand("sharptimer_postgres_enabled", "Whether player times should be put into a postgres database by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPostgresConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            usePostgres = bool.TryParse(args, out bool usePostgresValue) ? usePostgresValue : args != "0" && usePostgres;
        }

        [ConsoleCommand("sharptimer_discordwebhook_enabled", "Whether player PBs or SRs should be printed into a discord channel or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerDiscordWebhookConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            discordWebhookEnabled = bool.TryParse(args, out bool discordWebhookEnabledValue) ? discordWebhookEnabledValue : args != "0" && discordWebhookEnabled;
        }

        [ConsoleCommand("sharptimer_discordwebhook_print_sr", "Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerDiscordWebhookSRConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            discordWebhookPrintSR = bool.TryParse(args, out bool discordWebhookPrintSRValue) ? discordWebhookPrintSRValue : args != "0" && discordWebhookPrintSR;
        }

        [ConsoleCommand("sharptimer_discordwebhook_print_pb", "Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerDiscordWebhookPBConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            discordWebhookPrintPB = bool.TryParse(args, out bool discordWebhookPrintPBValue) ? discordWebhookPrintPBValue : args != "0" && discordWebhookPrintPB;
        }

        [ConsoleCommand("sharptimer_force_disable_json", "Whether player times should NOT be saved to JSON. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerIgnoreJSONConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            ignoreJSON = bool.TryParse(args, out bool ignoreJSONValue) ? ignoreJSONValue : args != "0" && ignoreJSON;
        }

        [ConsoleCommand("sharptimer_command_spam_cooldown", "Defines the time between commands can be called. Default value: 1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCmdCooldownConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float cooldown) && cooldown > 0)
            {
                cmdCooldown = (int)(cooldown * 64);
                SharpTimerConPrint($"SharpTimer command cooldown set to {cooldown} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid command cooldown value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_max_bhop_block_time", "Defines the time the player is allowed to stand on a Bhop block (if the map supports it). Default value: 1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerBhopBlockConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float time) && time > 0)
            {
                bhopBlockTime = (int)(time * 64);
                SharpTimerConPrint($"SharpTimer max bhop block time set to {time} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid max bhop block time value. Please provide a positive float.");
            }
        }

        [ConsoleCommand("sharptimer_respawn_enabled", "Whether !r is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRespawnConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            respawnEnabled = bool.TryParse(args, out bool respawnEnabledValue) ? respawnEnabledValue : args != "0" && respawnEnabled;
        }

        [ConsoleCommand("sharptimer_end_enabled", "Whether !end is enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRespawnEndConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            respawnEndEnabled = bool.TryParse(args, out bool respawnEndEnabledValue) ? respawnEndEnabledValue : args != "0" && respawnEndEnabled;
        }

        [ConsoleCommand("sharptimer_top_enabled", "Whether !top is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTopConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            topEnabled = bool.TryParse(args, out bool topEnabledValue) ? topEnabledValue : args != "0" && topEnabled;
        }

        [ConsoleCommand("sharptimer_help_enabled", "Whether !help is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHelpConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            helpEnabled = bool.TryParse(args, out bool helpEnabledValue) ? helpEnabledValue : args != "0" && helpEnabled;
        }

        [ConsoleCommand("sharptimer_rank_enabled", "Whether !rank is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRankConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (rankEnabledInitialized) return;

            string args = command.ArgString;
            rankEnabled = bool.TryParse(args, out bool rankEnabledValue) ? rankEnabledValue : args != "0" && rankEnabled;

            rankEnabledInitialized = true;
        }

        [ConsoleCommand("sharptimer_goto_enabled", "Whether !goto is enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGoToConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            goToEnabled = bool.TryParse(args, out bool goToEnabledValue) ? goToEnabledValue : args != "0" && goToEnabled;
        }

        [ConsoleCommand("sharptimer_remove_legs", "Whether Legs should be removed or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveLegsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeLegsEnabled = bool.TryParse(args, out bool removeLegsEnabledValue) ? removeLegsEnabledValue : args != "0" && removeLegsEnabled;
        }

        [ConsoleCommand("sharptimer_remove_damage", "Whether dealing damage should be disabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveDamageConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            disableDamage = bool.TryParse(args, out bool disableDamageValue) ? disableDamageValue : args != "0" && disableDamage;
        }

        [ConsoleCommand("sharptimer_remove_collision", "Whether Player collision should be removed or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveCollisionConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCollisionEnabled = bool.TryParse(args, out bool removeCollisionEnabledValue) ? removeCollisionEnabledValue : args != "0" && removeCollisionEnabled;
        }

        [ConsoleCommand("sharptimer_checkpoints_enabled", "Whether !cp, !tp and !prevcp are enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCPConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cpEnabled = bool.TryParse(args, out bool cpEnabledValue) ? cpEnabledValue : args != "0" && cpEnabled;
        }

        [ConsoleCommand("sharptimer_remove_checkpoints_restrictions", "Whether checkpoints should save in the air with the current player speed. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCPRestrictConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCpRestrictEnabled = bool.TryParse(args, out bool removeCpRestrictEnabledValue) ? removeCpRestrictEnabledValue : args != "0" && removeCpRestrictEnabled;
        }

        [ConsoleCommand("sharptimer_disable_telehop", "Whether the players speed should loose all speed when entring a teleport map trigger or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerResetTeleportSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            resetTriggerTeleportSpeedEnabled = bool.TryParse(args, out bool resetTriggerTeleportSpeedEnabledValue) ? resetTriggerTeleportSpeedEnabledValue : args != "0" && resetTriggerTeleportSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_max_start_speed_enabled", "Whether the players speed should be limited on exiting the starting trigger or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxStartSpeedBoolConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            maxStartingSpeedEnabled = bool.TryParse(args, out bool maxStartingSpeedEnabledValue) ? maxStartingSpeedEnabledValue : args != "0" && maxStartingSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_max_start_speed", "Defines max speed the player is allowed to have while exiting the start trigger. Default value: 320")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxStartSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int speed) && speed > 0)
            {
                maxStartingSpeed = speed;
                Server.ExecuteCommand($"sv_maxspeed {maxStartingSpeed}");
                SharpTimerConPrint($"SharpTimer max trigger speed set to {speed}.");
            }
            else
            {
                SharpTimerConPrint("Invalid max trigger speed value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_max_bonus_start_speed", "Defines max speed the player is allowed to have while exiting the start trigger. Default value: 320")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxBonusStartSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int speed) && speed > 0)
            {
                maxBonusStartingSpeed = speed;
                SharpTimerConPrint($"SharpTimer max bonus trigger speed set to {speed}.");
            }
            else
            {
                SharpTimerConPrint("Invalid max bonus trigger speed value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_force_knife_speed", "Whether the players speed should be always knife speed regardless of weapon held. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerForceKnifeSpeedBoolConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            forcePlayerSpeedEnabled = bool.TryParse(args, out bool forcePlayerSpeedEnabledValue) ? forcePlayerSpeedEnabledValue : args != "0" && forcePlayerSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_forced_player_speed", "Speed override for sharptimer_force_knife_speed. Default value: 250")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerForcedSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int speed) && speed > 0)
            {
                forcedPlayerSpeed = speed;
                SharpTimerConPrint($"SharpTimer forced player speed set to {speed}.");
            }
            else
            {
                SharpTimerConPrint("Invalid forced player speed value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_bhop_block_ticks", "Ticks allowed on bhop_block. Default value: 16")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerBhopBlockTicksConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int bhopTicks) && bhopTicks > 0)
            {
                bhopBlockTime = bhopTicks;
                SharpTimerConPrint($"SharpTimer forced bhop_block ticks to {bhopTicks}.");
            }
            else
            {
                SharpTimerConPrint("Invalid bhop_block ticks value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_stage_times_enabled", "Whether stage time records are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerStageTimeConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableStageTimes = bool.TryParse(args, out bool enableStageTimesValue) ? enableStageTimesValue : args != "0" && enableStageTimes;
        }

        [ConsoleCommand("sharptimer_connect_commands_msg_enabled", "Whether commands on join messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConnectCmdMSGConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cmdJoinMsgEnabled = bool.TryParse(args, out bool cmdJoinMsgEnabledValue) ? cmdJoinMsgEnabledValue : args != "0" && cmdJoinMsgEnabled;
        }

        [ConsoleCommand("sharptimer_connectmsg_enabled", "Whether connect/disconnect messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConnectMSGConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            connectMsgEnabled = bool.TryParse(args, out bool connectMsgEnabledValue) ? connectMsgEnabledValue : args != "0" && connectMsgEnabled;
        }

        [ConsoleCommand("sharptimer_remove_crouch_fatigue", "Whether the player should get no crouch fatigue or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveCrouchFatigueConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCrouchFatigueEnabled = bool.TryParse(args, out bool removeCrouchFatigueEnabledValue) ? removeCrouchFatigueEnabledValue : args != "0" && removeCrouchFatigueEnabled;
        }

        [ConsoleCommand("sharptimer_checkpoints_only_when_timer_stopped", "Will only allow checkpoints if timer is stopped using !timer")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCheckpointsOnlyWithStoppedTimer(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cpOnlyWhenTimerStopped = bool.TryParse(args, out bool cpOnlyWhenTimerStoppedValue) ? cpOnlyWhenTimerStoppedValue : args != "0" && cpOnlyWhenTimerStopped;
        }

        /* ad messages */
        [ConsoleCommand("sharptimer_ad_sr_enabled", "Whether to print sr message or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAdServerRecordEnabled(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            adServerRecordEnabled = bool.TryParse(args, out bool adSREnabledValue) ? adSREnabledValue : args != "0" && adServerRecordEnabled;
        }

        [ConsoleCommand("sharptimer_ad_sr_timer", "Interval how often the messages shall be printed to chat. Default value: 240")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAdServerRecordTimer(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int interval) && interval > 0)
            {
                adServerRecordTimer = interval;
                SharpTimerConPrint($"SharpTimer sr ad interval set to {interval} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid sr ad interval value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_ad_messages_enabled", "Whether to print ad message or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAdMessagesEnabled(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            adMessagesEnabled = bool.TryParse(args, out bool adCommandsEnabledValue) ? adCommandsEnabledValue : args != "0" && adMessagesEnabled;
        }

        [ConsoleCommand("sharptimer_ad_messages_timer", "Interval how often the messages shall be printed to chat. Default value: 120")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAdMessagesTimer(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int interval) && interval > 0)
            {
                adMessagesTimer = interval;
                SharpTimerConPrint($"SharpTimer messages ad interval set to {interval} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid messages ad interval value. Please provide a positive integer.");
            }
        }
        /* ad messages */

        [ConsoleCommand("sharptimer_hud_primary_color", "Primary Color for Timer HUD. Default value: green")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPrimaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                primaryHUDcolor = $"green";
                return;
            }

            primaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_hud_secondary_color_dynamic", "Whether to use dynamic color for secondary HUD based on player speed. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHUDSecondaryColorDynamicConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            useDynamicColor = bool.TryParse(args, out bool useDynamicColorValue) ? useDynamicColorValue : args != "0" && useDynamicColor;
        }

        [ConsoleCommand("sharptimer_hud_secondary_color", "Secondary Color for Timer HUD. Default value: orange")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSecondaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                secondaryHUDcolor = $"orange";
                return;
            }

            secondaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_hud_tertiary_color", "Tertiary Color for Timer HUD. Default value: white")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTertiaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                tertiaryHUDcolor = $"white";
                return;
            }

            tertiaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_fake_zones_height", "Fake Zones height in units. Default value: 50")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerFakeTriggerHeightConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, NumberStyles.Any, culture, out float height) && height > 0)
            {
                fakeTriggerHeight = height;
                SharpTimerConPrint($"SharpTimer fake trigger height set to {height} units.");
            }
            else
            {
                SharpTimerConPrint("Invalid fake trigger height value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_zones_box", "Make Zone a 3D Box. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerZones3DBox(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            Box3DZones = bool.TryParse(args, out bool ZoneBoxEnabledValue) ? ZoneBoxEnabledValue : args != "0" && Box3DZones;
        }

        [ConsoleCommand("sharptimer_allow_startzone_jump", "Enable or disable jumping in startzone. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerStartzoneJumpConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            startzoneJumping = bool.TryParse(args, out bool startzoneJumpingValue) ? startzoneJumpingValue : args != "0" && startzoneJumping;
        }

        [ConsoleCommand("sharptimer_spawn_on_respawnpos", "Teleports player to respawnpos on spawn. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSpawnOnRespawnPos(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            spawnOnRespawnPos = bool.TryParse(args, out bool spawnOnRespawnPosValue) ? spawnOnRespawnPosValue : args != "0" && spawnOnRespawnPos;
        }

        /* sounds convars */
        [ConsoleCommand("sharptimer_enable_sounds_by_default", "Whether to enable sounds for players by default.Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundEnableByDefault(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            soundsEnabledByDefault = bool.TryParse(args, out bool soundsEnabledByDefaultValue) ? soundsEnabledByDefaultValue : args != "0" && soundsEnabledByDefault;
        }

        [ConsoleCommand("sharptimer_sound_timer", "Defines Timer sound. Default value: sounds/ui/counter_beep.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundTimer(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            timerSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_respawn", "Defines Timer sound. Default value: sounds/ui/counter_beep.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundRespawn(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            respawnSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_checkpoint", "Defines Checkpoint sound. Default value: sounds/ui/buttonclick.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundCheckpoint(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            cpSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_checkpoint_error", "Defines Checkpoint Error sound. Default value: sounds/ui/weapon_cant_buy.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundCheckpointError(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            cpSoundError = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_teleport", "Defines Teleport sound. Default value: sounds/buttons/blip1.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundTeleport(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            tpSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_pb", "Defines PB Sound. Default value: sounds/buttons/bell1.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundPersonalBestRecord(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            pbSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_sr", "Defines SR Sound. Default value: sounds/ui/panorama/round_report_round_won_01.vsnd")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundServerRecord(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();

            srSound = $"{args}";
        }

        [ConsoleCommand("sharptimer_sound_sr_all_players", "Whether to play SR sound for all players. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundServerRecordAllPlayers(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            srSoundAll = bool.TryParse(args, out bool soundSRAllValue) ? soundSRAllValue : args != "0" && srSoundAll;
        }
        [ConsoleCommand("sharptimer_sound_stage_all_players", "Whether to play stage record sound for all players. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSoundStageRecordAllPlayers(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            stageSoundAll = bool.TryParse(args, out bool stageSoundAllValue) ? stageSoundAllValue : args != "0" && stageSoundAll;
        }
        /* sounds convars */

        [ConsoleCommand("sharptimer_enable_noclip", "Enable or disable noclip for regular players. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerEnableNoclipConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableNoclip = bool.TryParse(args, out bool enableNoclipValue) ? enableNoclipValue : args != "0" && enableNoclip;
        }

        [ConsoleCommand("sharptimer_styles_enabled", "Enable or disable styles. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerEnableStylesConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if(!isLinux)
            {
                enableStyles = false;
                return;
            }

            enableStyles = bool.TryParse(args, out bool enableStylesValue) ? enableStylesValue : args != "0" && enableStyles;
        }

        [ConsoleCommand("sharptimer_style_points_enabled", "Enable or disable points granted for style runs. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerEnableStylePointsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableStylePoints = bool.TryParse(args, out bool enableStylePointsValue) ? enableStylePointsValue : args != "0" && enableStylePoints;
        }
        
        [ConsoleCommand("sharptimer_style_multiplier_parachute", "Point modifier for parachute. Default value: 0.8")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerParachuteMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                parachutePointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer parachute point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid parachute point modifier. Please provide a positive integer.");
            }
        }
        
        [ConsoleCommand("sharptimer_style_multiplier_tas", "Point modifier for TAS. Default value: 0.0")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTASMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                tasPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer TAS point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid TAS point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_lowgrav", "Point modifier for lowgrav. Default value: 0.8")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerLowGravMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                lowgravPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer low grav point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid low grav point modifier. Please provide a positive integer.");
            }
        }
        [ConsoleCommand("sharptimer_style_multiplier_sideways", "Point modifier for sidways. Default value: 1.3")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSidewaysMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                sidewaysPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer sideways point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid sideways point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_onlyw", "Point modifier for onlyw. Default value: 1.33")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerOnlyWMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                onlywPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer onlyw point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid onlyw point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_onlya", "Point modifier for onlya. Default value: 1.33")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerOnlyAMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                onlyaPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer onlya point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid onlya point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_onlys", "Point modifier for onlys. Default value: 1.33")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerOnlySMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                onlysPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer onlys point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid onlys point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_onlyd", "Point modifier for onlyd. Default value: 1.33")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerOnlyDMultiplierConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                onlydPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer onlyd point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid onlyd point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_400vel", "Point modifier for 400vel. Default value: 1.5")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimer400velConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                velPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer 400vel point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid 400vel point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_highgrav", "Point modifier for 400vel. Default value: 1.1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHighGravConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                highgravPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer highgrav point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid highgrav point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_halfsideways", "Point modifier for 400vel. Default value: 1.3")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHalfSidewaysConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                halfSidewaysPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer halfsideways point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid halfsideways point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_style_multiplier_fastforward", "Point modifier for 400vel. Default value: 0.8")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerFastForwardConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (double.TryParse(args, NumberStyles.Any, culture, out double pointModifier) && pointModifier is >= 0 and <= 2)
            {
                fastForwardPointModifier = pointModifier;
                SharpTimerConPrint($"SharpTimer fastforward point modifier set to {pointModifier}.");
            }
            else
            {
                SharpTimerConPrint("Invalid fastforward point modifier. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_remote_data_bhop", "Override for bhop remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideBhop(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteBhopDataSource = $"https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/bhop_.json";
                return;
            }

            remoteBhopDataSource = $"{args}";
        }

        [ConsoleCommand("sharptimer_remote_data_kz", "Override for kz remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideKZ(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteKZDataSource = $"https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/kz_.json";
                return;
            }

            remoteKZDataSource = $"{args}";
        }

        [ConsoleCommand("sharptimer_remote_data_surf", "Override for surf remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideSurf(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteSurfDataSource = $"https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/surf_.json";
                return;
            }

            remoteSurfDataSource = $"{args}";
        }
        
        [ConsoleCommand("sharptimer_replay_data_directory", "Directory for replay data. Prepended with game dir. Default value: csgo cfg SharpTimer PlayerReplayData")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayDataDirectory(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString.Trim();
            
            if (string.IsNullOrEmpty(args))
            {
                playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData");
                return;
            }
            
            playerReplaysPath = Path.Join(gameDir, args);
        }
    }
}