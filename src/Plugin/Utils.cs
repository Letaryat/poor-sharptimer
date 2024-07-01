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

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Entities;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public async Task<(bool IsLatest, string LatestVersion)> IsLatestVersion()
        {
            try
            {
                string apiUrl = "https://api.github.com/repos/Letaryat/poor-sharptimer/releases/latest";
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("User-Agent", "request");

                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var options = jsonSerializerOptions;
                    var releaseInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                    string latestVersion = releaseInfo!["name"].ToString()!;

                    return (latestVersion! == ModuleVersion!, latestVersion!);
                }
                else
                {
                    SharpTimerError($"Failed to fetch data from GitHub API: {response.StatusCode}");
                    return (false, "null");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"An error occurred in IsLatestVersion: {ex.Message}");
                return (false, "null");
            }
        }

        public async void CheckForUpdate()
        {
            try
            {
                (bool isLatest, string latestVersion) = await IsLatestVersion();

                if (!isLatest && latestVersion != "null")
                {
                    for (int i = 0; i < 5; i++)
                    {
                        SharpTimerConPrint($"\u001b[33m----------------------------------------------------");
                        SharpTimerConPrint($"\u001b[33mPLUGIN VERSION DOES NOT MATCH LATEST GITHUB RELEASE");
                        SharpTimerConPrint($"\u001b[33mCURRENT VERSION: {ModuleVersion}");
                        SharpTimerConPrint($"\u001b[33mLATEST RELEASE VERSION: {latestVersion}");
                        SharpTimerConPrint($"\u001b[33mPLEASE CONSIDER UPDATING SOON!");
                    }
                    SharpTimerConPrint($"\u001b[33m----------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"An error occurred in CheckForUpdate: {ex.Message}");
            }
        }

        private void ADtimerServerRecord()
        {
            if (isADServerRecordTimerRunning) return;

            var timer = AddTimer(adServerRecordTimer, () =>
            {
                Task.Run(async () =>
                {
                    Dictionary<string, PlayerRecord> sortedRecords;
                    if (!useMySQL && !usePostgres)
                    {
                        SharpTimerDebug($"Getting Server Record AD using json");
                        sortedRecords = await GetSortedRecords();
                    }
                    else
                    {
                        SharpTimerDebug($"Getting Server Record AD using database");
                        sortedRecords = await GetSortedRecordsFromDatabase(100);
                    }

                    SharpTimerDebug($"Running Server Record AD...");

                    if (sortedRecords.Count == 0)
                    {
                        SharpTimerDebug($"No Server Records for this map yet!");
                        return;
                    }

                    Server.NextFrame(() => Server.PrintToChatAll($" {Localizer["prefix"]} {Localizer["current_sr", currentMapName!]}"));

                    var serverRecord = sortedRecords.FirstOrDefault();
                    string playerName = serverRecord.Value.PlayerName!; // Get the player name from the dictionary value
                    int timerTicks = serverRecord.Value.TimerTicks; // Get the timer ticks from the dictionary value
                    Server.NextFrame(() => Server.PrintToChatAll($" {Localizer["prefix"]} {Localizer["current_sr_player", playerName, FormatTime(timerTicks)]}"));

                    SortedCachedRecords = sortedRecords;
                });
            }, TimerFlags.REPEAT);

            isADServerRecordTimerRunning = true;
        }

        private void ADtimerMessages()
        {
            if (isADMessagesTimerRunning) return;

            var timer = AddTimer(adMessagesTimer, () =>
            {
                string[] adMessages = File.ReadAllLines(Path.Join(gameDir + "/csgo/cfg/SharpTimer/admessages.txt"));
                var nonEmptyAds = adMessages.Where(ad => !string.IsNullOrEmpty(ad) && !ad.TrimStart().StartsWith("//")).ToArray();
                Server.NextFrame(() => Server.PrintToChatAll($"{ReplaceAdMessagePlaceholders(nonEmptyAds[new Random().Next(nonEmptyAds.Length)])}"));
            }, TimerFlags.REPEAT);

            isADMessagesTimerRunning = true;
        }

        private string ReplaceAdMessagePlaceholders(string message)
        {
            var replacements = new Dictionary<string, string>
            {
                { "{prefix}",       $"{Localizer["prefix"]}" },
                { "{current_map}",  $"{Server.MapName}" },
                { "{max_players}",  $"{Server.MaxPlayers}" },
                { "{players}",      $"{Utilities.GetPlayers().Count()}" },
                { "{current_time}", $"{DateTime.Now.ToString("HH:mm:ss")}" },
                { "{current_date}", $"{DateTime.Now.ToString("dd.MMM.yyyy")}" },
                { "{red}",          $"{ChatColors.Red}" },
                { "{white}",        $"{ChatColors.White}" },
                { "{default}",      $"{ChatColors.Default}" },
                { "{darkred}",      $"{ChatColors.DarkRed}" },
                { "{green}",        $"{ChatColors.Green}" },
                { "{lightyellow}",  $"{ChatColors.LightYellow}" },
                { "{lightblue}",    $"{ChatColors.LightBlue}" },
                { "{olive}",        $"{ChatColors.Olive}" },
                { "{lime}",         $"{ChatColors.Lime}" },
                { "{lightpurple}",  $"{ChatColors.LightPurple}" },
                { "{purple}",       $"{ChatColors.Purple}" },
                { "{grey}",         $"{ChatColors.Grey}" },
                { "{yellow}",       $"{ChatColors.Yellow}" },
                { "{gold}",         $"{ChatColors.Gold}" },
                { "{silver}",       $"{ChatColors.Silver}" },
                { "{blue}",         $"{ChatColors.Blue}" },
                { "{darkblue}",     $"{ChatColors.DarkBlue}" },
                { "{bluegrey}",     $"{ChatColors.BlueGrey}" },
                { "{magenta}",      $"{ChatColors.Magenta}" },
                { "{lightred}",     $"{ChatColors.LightRed}" },
                { "{orange}",       $"{ChatColors.Orange}" }
            };

            foreach (var replacement in replacements)
                message = message.Replace(replacement.Key, replacement.Value);

            return message;
        }

        public void SharpTimerDebug(string msg)
        {
            if (enableDebug == true) Logger.LogInformation($"\u001b[33m[SharpTimerDebug] \u001b[37m{msg}");
        }

        public void SharpTimerError(string msg)
        {
            Logger.LogInformation($"\u001b[31m[SharpTimerERROR] \u001b[37m{msg}");
        }

        public void SharpTimerConPrint(string msg)
        {
            Logger.LogInformation($"\u001b[36m[SharpTimer] \u001b[37m{msg}");
        }

        public static string FormatTime(int ticks)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ticks / 64.0);

            string milliseconds = $"{(ticks % 64) * (1000.0 / 64.0):000}";

            int totalMinutes = (int)timeSpan.TotalMinutes;
            if (totalMinutes >= 60)
            {
                return $"{totalMinutes / 60:D1}:{totalMinutes % 60:D2}:{timeSpan.Seconds:D2}.{milliseconds}";
            }

            return $"{totalMinutes:D1}:{timeSpan.Seconds:D2}.{milliseconds}";
        }

        private static string FormatTimeDifference(int currentTicks, int previousTicks, bool noColor = false)
        {
            int differenceTicks = previousTicks - currentTicks;
            string sign = (differenceTicks > 0) ? "-" : "+";
            char signColor = (differenceTicks > 0) ? ChatColors.Green : ChatColors.Red;

            TimeSpan timeDifference = TimeSpan.FromSeconds(Math.Abs(differenceTicks) / 64.0);

            // Format seconds with three decimal points
            string secondsWithMilliseconds = $"{timeDifference.Seconds:D2}.{Math.Abs(differenceTicks) % 64 * (1000.0 / 64.0):000}";

            int totalDifferenceMinutes = (int)timeDifference.TotalMinutes;
            if (totalDifferenceMinutes >= 60)
            {
                return $"{(noColor ? "" : $"{signColor}")}{sign}{totalDifferenceMinutes / 60:D1}:{totalDifferenceMinutes % 60:D2}:{secondsWithMilliseconds}";
            }

            return $"{(noColor ? "" : $"{signColor}")}{sign}{totalDifferenceMinutes:D1}:{secondsWithMilliseconds}";
        }

        private static string FormatSpeedDifferenceFromString(string currentSpeed, string previousSpeed, bool noColor = false)
        {
            if (int.TryParse(currentSpeed, out int currentSpeedInt) && int.TryParse(previousSpeed, out int previousSpeedInt))
            {
                int difference = previousSpeedInt - currentSpeedInt;
                string sign = (difference > 0) ? "-" : "+";
                char signColor = (difference < 0) ? ChatColors.Green : ChatColors.Red;

                return $"{(noColor ? "" : $"{signColor}")}{sign}{Math.Abs(difference)}";
            }
            else
            {
                return "n/a";
            }
        }

        public double CalculatePoints(int timerTicks, int style)
        {
            double basePoints = 10000.0;
            double timeFactor = 0.0001;
            double tierMult = 0.1;
            double styleMult = GetStyleMultiplier(style);

            if (currentMapTier != null)
            {
                tierMult = (double)(currentMapTier * 0.1);
            }

            double points = basePoints / (timerTicks * timeFactor);
            return points * tierMult * styleMult;
        }

        public double CalculatePBPoints(int timerTicks, int style)
        {
            double basePoints = 10000.0;
            double timeFactor = 0.01;
            double tierMult = 0.1;
            double styleMult = GetStyleMultiplier(style);

            if (currentMapTier != null)
            {
                tierMult = (double)(currentMapTier * 0.1);
            }

            double points = basePoints / (timerTicks * timeFactor);
            return points * tierMult * styleMult;
        }

        static string ReplaceVars(string loc_string, params string[] args)
        {
            return string.Format(loc_string, args);
        }

        static string ParsePrefixColors(string input)
        {
            Dictionary<string, string> colorNameSymbolMap = new(StringComparer.OrdinalIgnoreCase)
             {
                 { "{white}", "" },
                 { "{darkred}", "" },
                 { "{purple}", "" },
                 { "{olive}", "" },
                 { "{lime}", "" },
                 { "{green}", "" },
                 { "{red}", "" },
                 { "{grey}", "" },
                 { "{orange}", "" },
                 { "{lightpurple}", "" },
                 { "{lightred}", "" }
             };

            foreach (var entry in colorNameSymbolMap)
            {
                input = input.Replace(entry.Key, entry.Value.ToString());
            }

            return input;
        }

        string ParseColorToSymbol(string input)
        {
            Dictionary<string, string> colorNameSymbolMap = new(StringComparer.OrdinalIgnoreCase)
             {
                 { "white", "" },
                 { "darkred", "" },
                 { "purple", "" },
                 { "darkgreen", "" },
                 { "lightgreen", "" },
                 { "green", "" },
                 { "red", "" },
                 { "lightgray", "" },
                 { "orange", "" },
                 { "darkpurple", "" },
                 { "lightred", "" }
             };

            string lowerInput = input.ToLower();

            if (colorNameSymbolMap.TryGetValue(lowerInput, out var symbol))
            {
                return symbol;
            }

            if (IsHexColorCode(input))
            {
                return ParseHexToSymbol(input);
            }

            return "\u0010";
        }

        bool IsHexColorCode(string input)
        {
            if (input.StartsWith("#") && (input.Length == 7 || input.Length == 9))
            {
                try
                {
                    Color color = ColorTranslator.FromHtml(input);
                    return true;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error parsing hex color code: {ex.Message}");
                }
            }
            else
            {
                SharpTimerError("Invalid hex color code format. Please check SharpTimer/config.cfg");
            }

            return false;
        }

        static string ParseHexToSymbol(string hexColorCode)
        {
            Color color = ColorTranslator.FromHtml(hexColorCode);

            Dictionary<string, string> predefinedColors = new Dictionary<string, string>
            {
                { "#FFFFFF", "" },  // White
                { "#8B0000", "" },  // Dark Red
                { "#800080", "" },  // Purple
                { "#006400", "" },  // Dark Green
                { "#00FF00", "" },  // Light Green
                { "#008000", "" },  // Green
                { "#FF0000", "" },  // Red
                { "#D3D3D3", "" },  // Light Gray
                { "#FFA500", "" },  // Orange
                { "#780578", "" },  // Dark Purple
                { "#FF4500", "" }   // Light Red
            };

            hexColorCode = hexColorCode.ToUpper();

            if (predefinedColors.TryGetValue(hexColorCode, out var colorName))
            {
                return colorName;
            }

            Color targetColor = ColorTranslator.FromHtml(hexColorCode);
            string closestColor = FindClosestColor(targetColor, predefinedColors.Keys);

            if (predefinedColors.TryGetValue(closestColor, out var symbol))
            {
                return symbol;
            }

            return "";
        }

        static string FindClosestColor(Color targetColor, IEnumerable<string> colorHexCodes)
        {
            double minDistance = double.MaxValue;
            string? closestColor = null;

            foreach (var hexCode in colorHexCodes)
            {
                Color color = ColorTranslator.FromHtml(hexCode);
                double distance = ColorDistance(targetColor, color);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = hexCode;
                }
            }

            return closestColor!;
        }

        static double ColorDistance(Color color1, Color color2)
        {
            int rDiff = color1.R - color2.R;
            int gDiff = color1.G - color2.G;
            int bDiff = color1.B - color2.B;

            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        public void DrawLaserBetween(Vector startPos, Vector endPos, string _color = "")
        {
            string beamColor;
            if (beamColorOverride == true)
            {
                beamColor = _color;
            }
            else
            {
                beamColor = primaryHUDcolor;
            }

            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
            if (beam == null)
            {
                SharpTimerDebug($"Failed to create beam...");
                return;
            }

            if (IsHexColorCode(beamColor))
            {
                beam.Render = ColorTranslator.FromHtml(beamColor);
            }
            else
            {
                beam.Render = Color.FromName(beamColor);
            }

            beam.Width = 1.5f;

            beam.Teleport(startPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;

            beam.DispatchSpawn();
            SharpTimerDebug($"Beam Spawned at S:{startPos} E:{beam.EndPos}");
        }

        public void DrawWireframe3D(Vector corner1, Vector corner8, string _color)
        {
            Vector corner2 = new(corner1.X, corner8.Y, corner1.Z);
            Vector corner3 = new(corner8.X, corner8.Y, corner1.Z);
            Vector corner4 = new(corner8.X, corner1.Y, corner1.Z);

            Vector corner5 = new(corner8.X, corner1.Y, corner8.Z);
            Vector corner6 = new(corner1.X, corner1.Y, corner8.Z);
            Vector corner7 = new(corner1.X, corner8.Y, corner8.Z);

            //top square
            DrawLaserBetween(corner1, corner2, _color);
            DrawLaserBetween(corner2, corner3, _color);
            DrawLaserBetween(corner3, corner4, _color);
            DrawLaserBetween(corner4, corner1, _color);

            //bottom square
            DrawLaserBetween(corner5, corner6, _color);
            DrawLaserBetween(corner6, corner7, _color);
            DrawLaserBetween(corner7, corner8, _color);
            DrawLaserBetween(corner8, corner5, _color);

            //connect them both to build a cube
            DrawLaserBetween(corner1, corner6, _color);
            DrawLaserBetween(corner2, corner7, _color);
            DrawLaserBetween(corner3, corner8, _color);
            DrawLaserBetween(corner4, corner5, _color);
        }

        private bool IsVectorInsideBox(Vector playerVector, Vector corner1, Vector corner2)
        {
            float minX = Math.Min(corner1.X, corner2.X);
            float minY = Math.Min(corner1.Y, corner2.Y);
            float minZ = Math.Min(corner1.Z, corner2.Z);

            float maxX = Math.Max(corner1.X, corner2.X);
            float maxY = Math.Max(corner1.Y, corner2.Y);
            float maxZ = Math.Max(corner1.Z, corner2.Z + fakeTriggerHeight);

            return playerVector.X >= minX && playerVector.X <= maxX &&
                   playerVector.Y >= minY && playerVector.Y <= maxY &&
                   playerVector.Z >= minZ && playerVector.Z <= maxZ;
        }

        static Vector CalculateMiddleVector(Vector corner1, Vector corner2)
        {
            if (corner1 == null || corner2 == null)
            {
                return new Vector(0, 0, 0);
            }

            float middleX = (corner1.X + corner2.X) / 2;
            float middleY = (corner1.Y + corner2.Y) / 2;
            float middleZ = (corner1.Z + corner2.Z) / 2;
            return new Vector(middleX, middleY, middleZ);
        }

        private static Vector ParseVector(string vectorString)
        {
            if (string.IsNullOrWhiteSpace(vectorString))
            {
                return new Vector(0, 0, 0);
            }

            const char separator = ' ';

            var values = vectorString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }

        private static QAngle ParseQAngle(string qAngleString)
        {
            if (string.IsNullOrWhiteSpace(qAngleString))
            {
                return new QAngle(0, 0, 0);
            }

            const char separator = ' ';

            var values = qAngleString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], out float pitch) &&
                float.TryParse(values[1], out float yaw) &&
                float.TryParse(values[2], out float roll))
            {
                return new QAngle(pitch, yaw, roll);
            }

            return new QAngle(0, 0, 0);
        }

        public static double Distance(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return 0;
            }

            double deltaX = vector1.X - vector2.X;
            double deltaY = vector1.Y - vector2.Y;
            double deltaZ = vector1.Z - vector2.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public static double Distance2D(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return 0;
            }

            double deltaX = vector1.X - vector2.X;
            double deltaY = vector1.Y - vector2.Y;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private static Vector Normalize(Vector vector)
        {
            float magnitude = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);

            if (magnitude > 0)
            {
                return new Vector(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
            }
            else
            {
                return vector;
            }
        }

        public static bool IsVectorHigherThan(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return false;
            }

            return vector1.Z >= vector2.Z;
        }

        public async Task<Dictionary<string, PlayerRecord>> GetSortedRecords(int bonusX = 0, string mapName = "")
        {
            string? currentMapNamee;
            if (string.IsNullOrEmpty(mapName))
                currentMapNamee = bonusX == 0 ? $"{currentMapName!}.json" : $"{currentMapName}_bonus{bonusX}.json";
            else
                currentMapNamee = mapName;

            string mapRecordsPath = Path.Combine(playerRecordsPath!, currentMapNamee);

            Dictionary<string, PlayerRecord> records;

            try
            {
                using (JsonDocument? jsonDocument = await LoadJson(mapRecordsPath)!)
                {
                    if (jsonDocument != null)
                    {
                        records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(jsonDocument.RootElement.GetRawText()) ?? [];
                    }
                    else
                    {
                        records = [];
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetSortedRecords: {ex.Message}");
                records = [];
            }

            var sortedRecords = records
                .OrderBy(record => record.Value.TimerTicks)
                .ToDictionary(record => record.Key, record => new PlayerRecord
                {
                    PlayerName = record.Value.PlayerName,
                    TimerTicks = record.Value.TimerTicks
                });

            return sortedRecords;
        }

        public async Task<(string, string, string)> GetMapRecordSteamID(int bonusX = 0, int top10 = 0)
        {
            string mapRecordsPath = Path.Combine(playerRecordsPath!, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            Dictionary<string, PlayerRecord> records;

            try
            {
                using (JsonDocument? jsonDocument = await LoadJson(mapRecordsPath)!)
                {
                    if (jsonDocument != null)
                    {
                        records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(jsonDocument.RootElement.GetRawText()) ?? [];
                    }
                    else
                    {
                        records = [];
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetSortedRecords: {ex.Message}");
                records = [];
            }

            string steamId64 = "null";
            string playerName = "null";
            string timerTicks = "null";

            if (top10 != 0 && top10 <= records.Count)
            {
                var sortedRecords = records.OrderBy(x => x.Value.TimerTicks).ToList();
                var record = sortedRecords[top10 - 1];
                steamId64 = record.Key;
                playerName = record.Value.PlayerName!;
                timerTicks = FormatTime(record.Value.TimerTicks);
            }
            else
            {
                var minTimerTicksRecord = records.OrderBy(x => x.Value.TimerTicks).FirstOrDefault();
                if (minTimerTicksRecord.Key != null)
                {
                    steamId64 = minTimerTicksRecord.Key;
                    playerName = minTimerTicksRecord.Value.PlayerName!;
                    timerTicks = FormatTime(minTimerTicksRecord.Value.TimerTicks);
                }
            }

            return (steamId64, playerName, timerTicks);
        }

        private async Task<(int? Tier, string? Type)> FindMapInfoFromHTTP(string url)
        {
            try
            {
                SharpTimerDebug($"Trying to fetch remote_data for {currentMapName} from {url}");

                var response = await httpClient.GetStringAsync(url);

                using (var jsonDocument = JsonDocument.Parse(response))
                {
                    if (jsonDocument.RootElement.TryGetProperty(currentMapName!, out var mapInfo))
                    {
                        int? tier = null;
                        string? type = null;

                        if (mapInfo.TryGetProperty("Tier", out var tierElement))
                        {
                            tier = tierElement.GetInt32();
                        }

                        if (mapInfo.TryGetProperty("Type", out var typeElement))
                        {
                            type = typeElement.GetString();
                        }

                        SharpTimerDebug($"Fetched remote_data success! {tier} {type}");

                        return (tier, type);
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error Getting Remote Data for {currentMapName}: {ex.Message}");
                return (null, null);
            }
        }

        private async Task GetMapInfo()
        {
            string mapInfoSource = GetMapInfoSource();
            var (mapTier, mapType) = await FindMapInfoFromHTTP(mapInfoSource);
            currentMapTier = mapTier;
            currentMapType = mapType;
            string tierString = currentMapTier != null ? $" | Tier: {currentMapTier}" : "";
            string typeString = currentMapType != null ? $" | {currentMapType}" : "";

            if (autosetHostname == true)
            {
                Server.NextFrame(() =>
                {
                    Server.ExecuteCommand($"hostname {defaultServerHostname}{tierString}{typeString}");
                    SharpTimerDebug($"SharpTimer Hostname Updated to: {ConVar.Find("hostname")!.StringValue}");
                });
            }
        }

        private string GetMapInfoSource()
        {
            return currentMapName switch
            {
                var name when name!.StartsWith("kz_") => remoteKZDataSource!,
                var name when name!.StartsWith("bhop_") => remoteBhopDataSource!,
                var name when name!.StartsWith("surf_") => remoteSurfDataSource!,
                _ => null
            } ?? remoteSurfDataSource!;
        }

        private void KillServerCommandEnts()
        {
            if (killServerCommands == true)
            {
                var pointServerCommands = Utilities.FindAllEntitiesByDesignerName<CPointServerCommand>("point_servercommand");

                foreach (var servercmd in pointServerCommands)
                {
                    if (servercmd == null) continue;
                    SharpTimerDebug($"Killed point_servercommand ent: {servercmd.Handle}");
                    servercmd.Remove();
                }
            }
        }

        private void OnMapStartHandler(string mapName)
        {
            try
            {
                Server.NextFrame(() =>
                {
                    SharpTimerDebug("OnMapStart:");
                    SharpTimerDebug("Executing SharpTimer/config");
                    Server.ExecuteCommand("sv_autoexec_mapname_cfg 0");
                    Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

                    //delay custom_exec so it executes after map exec
                    SharpTimerDebug("Creating custom_exec 1sec delay");
                    var custom_exec_delay = AddTimer(1.0f, () =>
                    {
                        SharpTimerDebug("Re-Executing SharpTimer/custom_exec");
                        Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                        //enforce sv_cheats 0 for dumb maps
                        Server.ExecuteCommand("sv_cheats 0");

                        if (execCustomMapCFG == true)
                        {
                            string MapExecFile = GetClosestMapCFGMatch();
                            if (!string.IsNullOrEmpty(MapExecFile))
                                Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                            else
                                SharpTimerError("MapExec Error: file name returned null");
                        }

                        if (hideAllPlayers == true) Server.ExecuteCommand($"mp_teammates_are_enemies 1");
                        if (enableSRreplayBot)
                        {
                            Server.NextFrame(() =>
                            {
                                Server.ExecuteCommand($"sv_hibernate_when_empty 0");
                                Server.ExecuteCommand($"bot_join_after_player 0");
                            });
                        }
                    });

                    if (enableReplays == true && enableSRreplayBot == true)
                    {
                        AddTimer(5.0f, () =>
                        {
                            if (ConVar.Find("mp_force_pick_time")!.GetPrimitiveValue<float>() == 1.0)
                                _ = SpawnReplayBot();
                            else
                            {
                                Server.PrintToChatAll($" {ChatColors.LightRed}Couldnt Spawn Replay bot!");
                                Server.PrintToChatAll($" {ChatColors.LightRed}Please make sure mp_force_pick_time is set to 1");
                                Server.PrintToChatAll($" {ChatColors.LightRed}in your custom_exec.cfg");
                                SharpTimerError("Couldnt Spawn Replay bot! Please make sure mp_force_pick_time is set to 1 in your custom_exec.cfg");
                            }
                        });
                    }

                    if (removeCrouchFatigueEnabled == true) Server.ExecuteCommand("sv_timebetweenducks 0");

                    //bonusRespawnPoses.Clear();
                    bonusRespawnAngs.Clear();
                    
                    if(!sqlCheck)
                    {
                        if(usePostgres) _ = Task.Run(async () => await CheckPostgresTablesAsync());
                        if(useMySQL) _ = Task.Run(async () => await CheckTablesAsync());
                        sqlCheck = true;
                    }

                    cpTriggers.Clear();         // make sure old data is flushed in case new map uses fake zones
                    cpTriggerCount = 0;
                    bonusCheckpointTriggers.Clear();
                    stageTriggers.Clear();
                    stageTriggerAngs.Clear();
                    stageTriggerPoses.Clear();

                    KillServerCommandEnts();
                });
            }
            catch (Exception ex)
            {
                SharpTimerError($"In OnMapStartHandler: {ex}");
            }
        }

        private void LoadMapData(string mapName)
        {
            try
            {
                currentMapName = mapName;
                totalBonuses = new int[11];
                bonusRespawnPoses.Clear();
                bonusRespawnAngs.Clear();
                string recordsFileName = $"SharpTimer/PlayerRecords/";
                playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);

                string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
                mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);

                string postgresConfigFileName = "SharpTimer/postgresConfig.json";
                postgresPath = Path.Join(gameDir + "/csgo/cfg", postgresConfigFileName);

                string discordConfigFileName = "SharpTimer/discordConfig.json";
                string discordCFGpath = Path.Join(gameDir + "/csgo/cfg", discordConfigFileName);

                string mapdataFileName = $"SharpTimer/MapData/{currentMapName}.json";
                string mapdataPath = Path.Join(gameDir + "/csgo/cfg", mapdataFileName);

                string bonusdataPath = $"{gameDir}/csgo/cfg/SharpTimer/MapData/";
                string[] files = Directory.GetFiles(bonusdataPath);

                string[] bonusdataFileNames = new string[11];
                string[] bonusdataPaths = new string[11];

                foreach (string file in files)
                {
                    if (file.Contains($"{currentMapName}_bonus1"))
                    {
                        totalBonuses[1] = 1;
                        bonusdataFileNames[1] = $"/SharpTimer/MapData/{currentMapName}_bonus1.json";
                        bonusdataPaths[1] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[1]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus2"))
                    {
                        totalBonuses[2] = 2;
                        bonusdataFileNames[2] = $"/SharpTimer/MapData/{currentMapName}_bonus2.json";
                        bonusdataPaths[2] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[2]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus3"))
                    {
                        totalBonuses[3] = 3;
                        SharpTimerDebug($"Found bonus 3 in {currentMapName}");
                        bonusdataFileNames[3] = $"/SharpTimer/MapData/{currentMapName}_bonus3.json";
                        bonusdataPaths[3] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[3]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus4"))
                    {
                        totalBonuses[4] = 4;
                        bonusdataFileNames[4] = $"/SharpTimer/MapData/{currentMapName}_bonus4.json";
                        bonusdataPaths[4] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[4]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus5"))
                    {
                        totalBonuses[5] = 5;
                        bonusdataFileNames[5] = $"/SharpTimer/MapData/{currentMapName}_bonus5.json";
                        bonusdataPaths[5] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[5]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus6"))
                    {
                        totalBonuses[6] = 6;
                        bonusdataFileNames[6] = $"/SharpTimer/MapData/{currentMapName}_bonus6.json";
                        bonusdataPaths[6] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[6]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus7"))
                    {
                        totalBonuses[7] = 7;
                        bonusdataFileNames[7] = $"/SharpTimer/MapData/{currentMapName}_bonus7.json";
                        bonusdataPaths[7] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[7]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus8"))
                    {
                        totalBonuses[8] = 8;
                        bonusdataFileNames[8] = $"/SharpTimer/MapData/{currentMapName}_bonus8.json";
                        bonusdataPaths[8] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[8]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus9"))
                    {
                        totalBonuses[9] = 9;
                        bonusdataFileNames[9] = $"/SharpTimer/MapData/{currentMapName}_bonus9.json";
                        bonusdataPaths[9] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[9]);
                    }
                    else if (file.Contains($"{currentMapName}_bonus10"))
                    {
                        totalBonuses[10] = 10;
                        bonusdataFileNames[10] = $"/SharpTimer/MapData/{currentMapName}_bonus10.json";
                        bonusdataPaths[10] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[10]);
                    }
                }
                Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");
                SharpTimerDebug("Re-Executing custom_exec with 1sec delay...");
                var custom_exec_delay = AddTimer(1.0f, () =>
                {
                    SharpTimerDebug("Re-Executing SharpTimer/custom_exec");
                    Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                    if (execCustomMapCFG == true)
                    {
                        string MapExecFile = GetClosestMapCFGMatch();
                        if (!string.IsNullOrEmpty(MapExecFile))
                            Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                        else
                            SharpTimerError("MapExec Error: file name returned null");
                    }

                    if (hideAllPlayers == true) Server.ExecuteCommand($"mp_teammates_are_enemies 1");
                    if (enableSRreplayBot)
                    {
                        Server.NextFrame(() =>
                        {
                            Server.ExecuteCommand($"sv_hibernate_when_empty 0");
                            Server.ExecuteCommand($"bot_join_after_player 0");
                        });
                    }
                });

                if (adServerRecordEnabled == true) ADtimerServerRecord();
                if (adMessagesEnabled == true) ADtimerMessages();

                entityCache = new EntityCache();
                UpdateEntityCache();

                _ = Task.Run(async () => { SortedCachedRecords = await GetSortedRecords(); });

                ClearMapData();

                _ = Task.Run(GetMapInfo);

                _ = Task.Run(async () => await GetDiscordWebhookURLFromConfigFile(discordCFGpath));

                primaryChatColor = ParseColorToSymbol(primaryHUDcolor);

                SharpTimerConPrint($"Trying to find Map data json for map: {currentMapName}!");
                //Bonus fake zone check
                foreach (int bonus in totalBonuses)
                {
                    if (bonus == 0) { }
                    else
                    {
                        using JsonDocument? bonusJsonDocument = LoadJsonOnMainThread(bonusdataPaths[bonus]);
                        if (bonusJsonDocument != null)
                        {
                            var mapInfo = JsonSerializer.Deserialize<MapInfo>(bonusJsonDocument.RootElement.GetRawText());
                            SharpTimerDebug($"Map data json found for map: {currentMapName}, bonus {bonus}!");

                            if (!string.IsNullOrEmpty(mapInfo!.BonusStartC1) && !string.IsNullOrEmpty(mapInfo.BonusStartC2) && !string.IsNullOrEmpty(mapInfo.BonusEndC1) && !string.IsNullOrEmpty(mapInfo.BonusEndC2) && !string.IsNullOrEmpty(mapInfo.BonusRespawnPos))
                            {
                                useTriggers = false;
                                if (FindEndTriggerPos() != null)
                                    useTriggersAndFakeZones = true;
                                SharpTimerConPrint($"useTriggers: {useTriggers}!");
                                currentBonusStartC1[bonus] = ParseVector(mapInfo.BonusStartC1);
                                currentBonusStartC2[bonus] = ParseVector(mapInfo.BonusStartC2);
                                currentBonusEndC1[bonus] = ParseVector(mapInfo.BonusEndC1);
                                currentBonusEndC2[bonus] = ParseVector(mapInfo.BonusEndC2);
                                currentBonusEndPos[bonus] = CalculateMiddleVector(currentBonusEndC1[bonus], currentBonusEndC2[bonus]);
                                bonusRespawnPoses[bonus] = ParseVector(mapInfo.BonusRespawnPos);
                                SharpTimerConPrint($"Found Fake Bonus {bonus} Trigger Corners: START {currentBonusStartC1[bonus]}, {currentBonusStartC2[bonus]} | END {currentBonusEndC1[bonus]}, {currentBonusEndC2[bonus]}");
                            }
                            if (currentBonusStartC1[bonus] != null && currentBonusStartC2[bonus] != null && currentBonusEndC1[bonus] != null && currentBonusEndC2[bonus] != null)
                            {
                                DrawWireframe3D(currentBonusStartC1[bonus], currentBonusStartC2[bonus], startBeamColor);
                                DrawWireframe3D(currentBonusEndC1[bonus], currentBonusEndC2[bonus], endBeamColor);
                            }
                        }
                    }
                }
                //Main fake zone check
                using JsonDocument? jsonDocument = LoadJsonOnMainThread(mapdataPath);
                if (jsonDocument != null)
                {
                    var mapInfo = JsonSerializer.Deserialize<MapInfo>(jsonDocument.RootElement.GetRawText());
                    SharpTimerConPrint($"Map data json found for map: {currentMapName}!");

                    if (!string.IsNullOrEmpty(mapInfo!.MapStartC1) && !string.IsNullOrEmpty(mapInfo.MapStartC2) && !string.IsNullOrEmpty(mapInfo.MapEndC1) && !string.IsNullOrEmpty(mapInfo.MapEndC2))
                    {
                        useTriggers = false;
                        SharpTimerConPrint($"useTriggers: {useTriggers}!");
                        currentMapStartC1 = ParseVector(mapInfo.MapStartC1);
                        currentMapStartC2 = ParseVector(mapInfo.MapStartC2);
                        currentMapEndC1 = ParseVector(mapInfo.MapEndC1);
                        currentMapEndC2 = ParseVector(mapInfo.MapEndC2);
                        currentEndPos = CalculateMiddleVector(currentMapEndC1, currentMapEndC2);
                        SharpTimerConPrint($"Found Fake Trigger Corners: START {currentMapStartC1}, {currentMapStartC2} | END {currentMapEndC1}, {currentMapEndC2}");
                    }

                    if (!string.IsNullOrEmpty(mapInfo.MapStartTrigger) && !string.IsNullOrEmpty(mapInfo.MapEndTrigger))
                    {
                        currentMapStartTrigger = mapInfo.MapStartTrigger;
                        currentMapEndTrigger = mapInfo.MapEndTrigger;
                        useTriggers = true;
                        SharpTimerConPrint($"Found Trigger Names: START {currentMapStartTrigger} | END {currentMapEndTrigger}");
                    }

                    if (!string.IsNullOrEmpty(mapInfo.RespawnPos))
                    {
                        currentRespawnPos = ParseVector(mapInfo.RespawnPos);
                        SharpTimerConPrint($"Found RespawnPos: {currentRespawnPos}");
                    }
                    else
                    {
                        (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();
                        currentEndPos = FindEndTriggerPos();
                        FindBonusStartTriggerPos();
                        SharpTimerConPrint($"RespawnPos not found, trying to hook trigger pos instead");
                        if (currentRespawnPos == null)
                        {
                            SharpTimerConPrint($"Hooking Trigger RespawnPos Failed!");
                        }
                        else
                        {
                            SharpTimerConPrint($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");
                        }
                    }

                    if (mapInfo.OverrideDisableTelehop != null && mapInfo.OverrideDisableTelehop.Length != 0)
                    {
                        try
                        {
                            currentMapOverrideDisableTelehop = mapInfo.OverrideDisableTelehop
                                .Split(',')
                                .Select(trigger => trigger.Trim())
                                .ToArray();

                            SharpTimerConPrint($"Overriding OverrideDisableTelehop...");
                        }
                        catch (FormatException)
                        {
                            SharpTimerError("Invalid string format for OverrideDisableTelehop... Example: 's1_end, s2_end, s3_end, s4_end, s5_end, s6_end, s7_end, s8_end'");
                        }
                    }
                    else
                    {
                        currentMapOverrideStageRequirement = false;
                    }

                    if (mapInfo.OverrideMaxSpeedLimit != null && mapInfo.OverrideMaxSpeedLimit.Length != 0)
                    {
                        try
                        {
                            SharpTimerConPrint($"Overriding MaxSpeedLimit...");
                            currentMapOverrideMaxSpeedLimit = mapInfo.OverrideMaxSpeedLimit
                                .Split(',')
                                .Select(trigger => trigger.Trim())
                                .ToArray();

                            foreach (var trigger in currentMapOverrideMaxSpeedLimit)
                            {
                                SharpTimerConPrint($"OverrideMaxSpeedLimit for trigger: {trigger}");
                            }

                        }
                        catch (Exception ex)
                        {
                            SharpTimerError($"Error parsing OverrideMaxSpeedLimit array: {ex.Message}");
                        }
                    }
                    else
                    {
                        currentMapOverrideMaxSpeedLimit = [];
                    }

                    if (!string.IsNullOrEmpty(mapInfo.OverrideStageRequirement))
                    {
                        try
                        {
                            currentMapOverrideStageRequirement = bool.Parse(mapInfo.OverrideStageRequirement);
                            SharpTimerConPrint($"Overriding StageRequirement...");
                        }
                        catch (FormatException)
                        {
                            SharpTimerError("Invalid boolean string format for OverrideStageRequirement");
                        }
                    }
                    else
                    {
                        currentMapOverrideStageRequirement = false;
                    }

                    if (!string.IsNullOrEmpty(mapInfo.OverrideTriggerPushFix))
                    {
                        try
                        {
                            currentMapOverrideTriggerPushFix = bool.Parse(mapInfo.OverrideTriggerPushFix);
                            SharpTimerConPrint($"Overriding TriggerPushFix...");
                        }
                        catch (FormatException)
                        {
                            SharpTimerError("Invalid boolean string format for OverrideTriggerPushFix");
                        }
                    }
                    else
                    {
                        currentMapOverrideTriggerPushFix = false;
                    }

                    if (!string.IsNullOrEmpty(mapInfo.GlobalPointsMultiplier))
                    {
                        try
                        {
                            globalPointsMultiplier = float.Parse(mapInfo.GlobalPointsMultiplier);
                            SharpTimerConPrint($"Set global points multiplier to x{globalPointsMultiplier}");
                        }
                        catch (FormatException)
                        {
                            SharpTimerError("Invalid float string format for GlobalPointsMultiplier");
                        }
                    }

                    if (!string.IsNullOrEmpty(mapInfo.MapTier))
                    {
                        AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                        {
                            try
                            {
                                currentMapTier = int.Parse(mapInfo.MapTier);
                                SharpTimerConPrint($"Overriding MapTier to {currentMapTier}");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid int string format for MapTier");
                            }
                        });
                    }

                    if (!string.IsNullOrEmpty(mapInfo.MapType))
                    {
                        AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                        {
                            try
                            {
                                currentMapType = mapInfo.MapType;
                                SharpTimerConPrint($"Overriding MapType to {currentMapType}");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid string format for MapType");
                            }
                        });
                    }

                    if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null)
                    {
                        DrawWireframe3D(currentMapStartC1, currentMapStartC2, startBeamColor);
                        DrawWireframe3D(currentMapEndC1, currentMapEndC2, endBeamColor);
                    }
                    else
                    {
                        var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                        if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                        DrawWireframe3D(startRight, startLeft, startBeamColor);
                        DrawWireframe3D(endRight, endLeft, endBeamColor);
                    }

                    if (triggerPushFixEnabled == true && currentMapOverrideTriggerPushFix == false)
                        FindTriggerPushData();

                    if (useTriggers == true || useTriggersAndFakeZones == true)
                    {
                        FindStageTriggers();
                        FindCheckpointTriggers();
                        FindBonusCheckpointTriggers();
                    }

                    KillServerCommandEnts();
                }
                else
                {
                    SharpTimerConPrint($"Map data json not found for map: {currentMapName}!");
                    SharpTimerConPrint($"Trying to hook Triggers supported by default!");

                    if (triggerPushFixEnabled == true && currentMapOverrideTriggerPushFix == false)
                        FindTriggerPushData();

                    KillServerCommandEnts();

                    (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();
                    currentEndPos = FindEndTriggerPos();
                    FindBonusStartTriggerPos();
                    FindStageTriggers();
                    FindCheckpointTriggers();
                    FindBonusCheckpointTriggers();

                    if (currentRespawnPos == null)
                        SharpTimerConPrint($"Hooking Trigger RespawnPos Failed!");
                    else
                        SharpTimerConPrint($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");

                    if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null && useTriggersAndFakeZones == false)
                    {
                        DrawWireframe3D(currentMapStartC1, currentMapStartC2, startBeamColor);
                        DrawWireframe3D(currentMapEndC1, currentMapEndC2, endBeamColor);
                    }
                    else
                    {
                        var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                        if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                        DrawWireframe3D(startRight, startLeft, startBeamColor);
                        DrawWireframe3D(endRight, endLeft, endBeamColor);
                    }

                    useTriggers = true;
                }

                SharpTimerConPrint($"useTriggers: {useTriggers}!");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in LoadMapData: {ex.Message}");
            }
        }

        public void ClearMapData()
        {
            cpTriggers.Clear();
            cpTriggerCount = 0;
            bonusCheckpointTriggers.Clear();
            stageTriggers.Clear();
            stageTriggerAngs.Clear();
            stageTriggerPoses.Clear();

            stageTriggerCount = 0;
            useStageTriggers = false;

            useTriggers = true;
            useTriggersAndFakeZones = false;

            currentMapStartC1 = new Vector(nint.Zero);
            currentMapStartC2 = new Vector(nint.Zero);
            currentMapEndC1 = new Vector(nint.Zero);
            currentMapEndC2 = new Vector(nint.Zero);

            currentRespawnPos = null;
            currentRespawnAng = null;

            currentEndPos = null;

            currentBonusStartC1 = new Vector[10];
            currentBonusStartC2 = new Vector[10];
            currentBonusEndC1 = new Vector[10];
            currentBonusEndC2 = new Vector[10];

            // totalBonuses = new int[10];
            currentMapStartTriggerMaxs = null;
            currentMapStartTriggerMins = null;

            currentMapTier = null; //making sure previous map tier and type are wiped
            currentMapType = null;
            currentMapOverrideDisableTelehop = []; //making sure previous map overrides are reset
            currentMapOverrideMaxSpeedLimit = [];
            currentMapOverrideStageRequirement = false;
            currentMapOverrideTriggerPushFix = false;

            globalPointsMultiplier = 1.0f;

            startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = false;
        }

        public async Task<JsonDocument?> LoadJson(string path)
        {
            return await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        return JsonDocument.Parse(json);
                    }
                    catch (Exception ex)
                    {
                        SharpTimerError($"Error parsing JSON file: {path}, Error: {ex.Message}");
                    }
                }
                return null;
            });
        }

        public JsonDocument? LoadJsonOnMainThread(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonDocument.Parse(json);
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error parsing JSON file: {path}, Error: {ex.Message}");
                }
            }

            return null;
        }

        public string RemovePlayerTags(string input)
        {
            string originalTag = input;
            string[] playerTagsToRemove = [   $"[{customVIPTag}]",
                                                "[Unranked]",
                                                "[Silver I]", "[Silver II]", "[Silver III]",
                                                "[Gold I]", "[Gold II]", "[Gold III]",
                                                "[Platinum I]", "[Platinum II]", "[Platinum III]",
                                                "[Diamond I]", "[Diamond II]", "[Diamond III]",
                                                "[Master I]", "[Master II]", "[Master III]",
                                                "[Legend I]", "[Legend II]", "[Legend III]",
                                                "[Royalty I]", "[Royalty II]", "[Royalty III]",
                                                "[God I]", "[God II]", "[God III]"
                                            ];

            if (!string.IsNullOrEmpty(input))
            {
                foreach (var strToRemove in playerTagsToRemove)
                {
                    if (input.Contains(strToRemove))
                    {
                        input = Regex.Replace(input, Regex.Escape(strToRemove), string.Empty, RegexOptions.IgnoreCase).Trim();
                    }
                }
            }

            SharpTimerDebug($"Removing tags... I: {originalTag} O: {input}");

            return input;
        }

        static string FormatOrdinal(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 13)
            {
                return number + "th";
            }

            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th",
            };
        }

        static int GetNumberBeforeSlash(string input)
        {
            string[] parts = input.Split('/');

            if (parts.Length == 2 && int.TryParse(parts[0], out int result))
            {
                return result;
            }
            else
            {
                return -1;
            }
        }

        public string GetClosestMapCFGMatch()
        {
            try
            {
                if (gameDir == null)
                {
                    SharpTimerError("gameDir is not initialized.");
                    return "null";
                }

                string[] configFiles;
                try
                {
                    configFiles = Directory.GetFiles(Path.Combine(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "MapExecs"), "*.cfg");
                }
                catch (Exception ex)
                {
                    SharpTimerError("Error accessing MapExec directory: " + ex.Message);
                    return "null";
                }

                if (configFiles == null || configFiles.Length == 0)
                {
                    SharpTimerError("No MapExec files found.");
                    return "null";
                }

                string closestMatch = string.Empty;
                int closestMatchLength = int.MaxValue;

                foreach (string file in configFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    if (Server.MapName == fileName)
                    {
                        return fileName + ".cfg";
                    }

                    if (Server.MapName.StartsWith(fileName) && fileName.Length < closestMatchLength)
                    {
                        closestMatch = fileName + ".cfg";
                        closestMatchLength = fileName.Length;
                    }
                }

                if (closestMatch == null || closestMatch == string.Empty)
                {
                    SharpTimerError("No closest MapExec match found.");
                    return "null";
                }

                return closestMatch;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error GetClosestMapCFGMatch: {ex.StackTrace}");
                return "null";
            }
        }
    }
}