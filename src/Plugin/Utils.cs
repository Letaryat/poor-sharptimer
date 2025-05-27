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
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Localization;

namespace SharpTimer
{
    public class Utils
    {
        private static readonly SharpTimer Plugin = SharpTimer.Instance;
        private static IStringLocalizer Localizer = Plugin.Localizer;

        private static string? gameDir = Plugin.gameDir;
        private static string? currentMapName = Plugin.currentMapName;
        private static int? currentMapTier = Plugin.currentMapTier;
        private static string? currentMapType = Plugin.currentMapType;

        private static float fakeTriggerHeight = Plugin.fakeTriggerHeight;
        private static bool Box3DZones = Plugin.Box3DZones;
        private static bool enableReplays = Plugin.enableReplays;

        private delegate nint CNetworkSystemUpdatePublicIp(nint a1);
        private static CNetworkSystemUpdatePublicIp? _networkSystemUpdatePublicIp;
        private static readonly HttpClient httpClient = new();
        public static JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static async Task<(bool IsLatest, string LatestVersion)> IsLatestVersion()
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

                    return (latestVersion! == Plugin.ModuleVersion!, latestVersion!);
                }
                else
                {
                    LogError($"Failed to fetch data from GitHub API: {response.StatusCode}");
                    return (false, "null");
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred in IsLatestVersion: {ex.Message}");
                return (false, "null");
            }
        }

        public static async void CheckForUpdate()
        {
            try
            {
                (bool isLatest, string latestVersion) = await IsLatestVersion();

                if (!isLatest && latestVersion != "null")
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ConPrint($"\u001b[33m----------------------------------------------------");
                        ConPrint($"\u001b[33mPLUGIN VERSION DOES NOT MATCH LATEST GITHUB RELEASE");
                        ConPrint($"\u001b[33mCURRENT VERSION: {Plugin.ModuleVersion}");
                        ConPrint($"\u001b[33mLATEST RELEASE VERSION: {latestVersion}");
                        ConPrint($"\u001b[33mPLEASE CONSIDER UPDATING SOON!");
                    }
                    ConPrint($"\u001b[33m----------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred in CheckForUpdate: {ex.Message}");
            }
        }

        public static string ReplaceVars(string message)
        {
            var replacements = new Dictionary<string, string>
            {
                { "{prefix}",       $"{Localizer["prefix"]}" },
                { "{current_map}",  $"{Server.MapName}" },
                { "{max_players}",  $"{Server.MaxPlayers}" },
                { "{players}",      $"{Utilities.GetPlayers().Count()}" },
                { "{current_time}", $"{DateTime.Now.ToString("HH:mm:ss")}" },
                { "{current_date}", $"{DateTime.Now.ToString("dd.MMM.yyyy")}" },
                { "{primary}",      $"{Plugin.primaryChatColor}" },
                { "{default}",      $"{ChatColors.Default}" },
                { "{red}",          $"{ChatColors.Red}" },
                { "{white}",        $"{ChatColors.White}" },
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

        public static void LogDebug(string msg)
        {
            if (Plugin.enableDebug == true) Plugin.Logger.LogDebug($"\u001b[33m[LogDebug] \u001b[37m{msg}");
        }

        public static void LogError(string msg)
        {
            Plugin.Logger.LogError($"\u001b[31m[LogError] \u001b[37m{msg}");
        }

        public static void ConPrint(string msg)
        {
            Plugin.Logger.LogInformation($"\u001b[36m[SharpTimer] \u001b[37m{msg}");
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

        public static string FormatTimeDifference(int currentTicks, int previousTicks, bool noColor = false)
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

        public static string FormatSpeedDifferenceFromString(string currentSpeed, string previousSpeed, bool noColor = false)
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

        public static string ParseColorToSymbol(string input)
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

        static bool IsHexColorCode(string input)
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
                    LogError($"Error parsing hex color code: {ex.Message}");
                }
            }
            else
            {
                LogError("Invalid hex color code format. Please check SharpTimer/config.cfg");
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

        public static void DrawLaserBetween(Vector startPos, Vector endPos, string _color = "")
        {
            string beamColor;
            if (Plugin.beamColorOverride == true)
            {
                beamColor = _color;
            }
            else
            {
                beamColor = Plugin.primaryHUDcolor;
            }

            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
            if (beam == null)
            {
                LogDebug($"Failed to create beam...");
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
            LogDebug($"Beam Spawned at S:{startPos} E:{beam.EndPos}");
        }

        public static void DrawWireframe3D(Vector corner1, Vector corner8, string _color)
        {
            Vector corner2 = new(corner1.X, corner8.Y, corner1.Z);
            Vector corner3 = new(corner8.X, corner8.Y, corner1.Z);
            Vector corner4 = new(corner8.X, corner1.Y, corner1.Z);

            Vector corner5 = new(corner8.X, corner1.Y, corner8.Z + (Box3DZones ? fakeTriggerHeight : 0));
            Vector corner6 = new(corner1.X, corner1.Y, corner8.Z + (Box3DZones ? fakeTriggerHeight : 0));
            Vector corner7 = new(corner1.X, corner8.Y, corner8.Z + (Box3DZones ? fakeTriggerHeight : 0));
            if (Plugin.Box3DZones) corner8 = new(corner8.X, corner8.Y, corner8.Z + Plugin.fakeTriggerHeight);

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

        public static bool IsVectorInsideBox(Vector playerVector, Vector corner1, Vector corner2)
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

        public static Vector CalculateMiddleVector(Vector corner1, Vector corner2)
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

        public static Vector ParseVector(string vectorString)
        {
            if (string.IsNullOrWhiteSpace(vectorString))
            {
                return new Vector(0, 0, 0);
            }

            const char separator = ' ';

            var values = vectorString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }

        public static QAngle ParseQAngle(string qAngleString)
        {
            if (string.IsNullOrWhiteSpace(qAngleString))
            {
                return new QAngle(0, 0, 0);
            }

            const char separator = ' ';

            var values = qAngleString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float pitch) &&
                float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float yaw) &&
                float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float roll))
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

            string mapRecordsPath = Path.Combine(Plugin.playerRecordsPath!, currentMapNamee);

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
                LogError($"Error in GetSortedRecords: {ex.Message}");
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

        public static PlayerRecord GetRecordByPosition(Dictionary<string, PlayerRecord> sortedRecords, int position)
        {
            if (position < 1 || position > sortedRecords.Count)
            {
                return null!;
            }

            var recordsList = sortedRecords.Values.ToList();
            for (int i = 0; i < recordsList.Count; i++)
            {
                if (i + 1 == position)
                {
                    return recordsList[i];
                }
            }
            return null!; 
        }

        public static async Task<(int? Tier, string? Type)> FindMapInfoFromHTTP(string url, string mapname = "")
        {
            try
            {
                if (mapname == "")
                    mapname = currentMapName!;
                LogDebug($"Trying to fetch remote_data for {currentMapName} from {url}");

                var response = await httpClient.GetStringAsync(url);

                using (var jsonDocument = JsonDocument.Parse(response))
                {
                    if (jsonDocument.RootElement.TryGetProperty(mapname, out var mapInfo))
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

                        LogDebug($"Fetched remote_data success! {tier} {type}");

                        return (tier, type);
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                LogError($"Error Getting Remote Data for {currentMapName}: {ex.Message}");
                return (null, null);
            }
        }

        public static async Task<(int? Tier, string? Type)> FindMapInfoFromLocal(string path, string mapname = "")
        {
            try
            {
                if (mapname == "")
                    mapname = currentMapName!;
                LogDebug($"Trying to fetch local_data for {currentMapName} from {path}");

                using (var jsonDocument = await LoadJson(path))
                {
                    
                    if (jsonDocument!.RootElement.TryGetProperty(mapname, out var mapInfo))
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

                        LogDebug($"Fetched local_data success! {tier} {type}");

                        return (tier, type);
                    } 
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                LogError($"Error Getting local_data for {currentMapName}: {ex.Message}");
                return (null, null);
            }
        }

        public static async Task GetMapInfo()
        {
            string mapInfoSource = GetMapInfoSource();
            int? mapTier;
            string? mapType;

            if (Plugin.disableRemoteData)
                (mapTier, mapType) = await FindMapInfoFromLocal(mapInfoSource);
            else
                (mapTier, mapType) = await FindMapInfoFromHTTP(mapInfoSource);
                
            currentMapTier = mapTier;
            currentMapType = mapType;
            string tierString = currentMapTier != null ? $" | Tier: {currentMapTier}" : "";
            string typeString = currentMapType != null ? $" | {currentMapType}" : "";

            if (Plugin.autosetHostname == true)
            {
                Server.NextFrame(() =>
                {
                    Server.ExecuteCommand($"hostname {Plugin.defaultServerHostname}{tierString}{typeString}");
                    LogDebug($"SharpTimer Hostname Updated to: {ConVar.Find("hostname")!.StringValue}");
                });
            }
        }

        public static string GetMapInfoSource()
        {
            if (Plugin.disableRemoteData)
            {
                return currentMapName switch
                {
                    var name when name!.StartsWith("kz_") => Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "local_data", "kz_.json")!,
                    var name when name!.StartsWith("bhop_") => Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "local_data", "bhop_.json")!,
                    var name when name!.StartsWith("surf_") => Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "local_data", "surf_.json"),
                    _ => null
                } ?? Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "local_data", "surf_.json");
            }
            return Plugin.currentMapName switch
            {
                var name when name!.StartsWith("kz_") => Plugin.remoteKZDataSource!,
                var name when name!.StartsWith("bhop_") => Plugin.remoteBhopDataSource!,
                var name when name!.StartsWith("surf_") => Plugin.remoteSurfDataSource!,
                _ => null
            } ?? Plugin.remoteSurfDataSource!;
        }

        public static void KillServerCommandEnts()
        {
            if (Plugin.killServerCommands == true)
            {
                var pointServerCommands = Utilities.FindAllEntitiesByDesignerName<CPointServerCommand>("point_servercommand");

                foreach (var servercmd in pointServerCommands)
                {
                    if (servercmd == null) continue;
                    LogDebug($"Killed point_servercommand ent: {servercmd.Handle}");
                    servercmd.Remove();
                }
            }
        }

        public static async Task<JsonDocument?> LoadJson(string path)
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
                        LogError($"Error parsing JSON file: {path}, Error: {ex.Message}");
                    }
                }
                return null;
            });
        }

        public static JsonDocument? LoadJsonOnMainThread(string path)
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
                    LogError($"Error parsing JSON file: {path}, Error: {ex.Message}");
                }
            }

            return null;
        }

        public static string RemovePlayerTags(string input)
        {
            string originalTag = input;

            List<string> playerTagsToRemove = [$"{Plugin.customVIPTag}", $"{Plugin.UnrankedTitle}"];

            foreach (var rank in Plugin.rankDataList)
                playerTagsToRemove.Add(rank.Title);

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

            LogDebug($"Removing tags... I: {originalTag} O: {input}");

            return input;
        }

        public static string FormatOrdinal(int number)
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

        public static int GetNumberBeforeSlash(string input)
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

        public static string GetClosestMapCFGMatch()
        {
            try
            {
                if (gameDir == null)
                {
                    LogError("gameDir is not initialized.");
                    return "null";
                }

                string[] configFiles;
                try
                {
                    configFiles = Directory.GetFiles(Path.Combine(gameDir, "csgo", "cfg", "SharpTimer", "MapData", "MapExecs"), "*.cfg");
                }
                catch (Exception ex)
                {
                    LogError("Error accessing MapExec directory: " + ex.Message);
                    return "null";
                }

                if (configFiles == null || configFiles.Length == 0)
                {
                    LogError("No MapExec files found.");
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
                    LogError("No closest MapExec match found.");
                    return "null";
                }

                return closestMatch;
            }
            catch (Exception ex)
            {
                LogError($"Error GetClosestMapCFGMatch: {ex.StackTrace}");
                return "null";
            }
        }

        public static bool IsApproximatelyEqual(float actual, float expected, float tolerance = 0.01f)
        {
            return Math.Abs(actual - expected) < tolerance;
        }

        // https://github.com/daffyyyy/CS2-SimpleAdmin/blob/main/CS2-SimpleAdmin/Helper.cs#L457C5-L481C6
        // remember, dont reinvent the wheel
        public static string GetServerIp()
        {
            var networkSystem = NativeAPI.GetValveInterface(0, "NetworkSystemVersion001");

            unsafe
            {
                if (_networkSystemUpdatePublicIp == null)
                {
                    var funcPtr = *(nint*)(*(nint*)(networkSystem) + 256);
                    _networkSystemUpdatePublicIp = Marshal.GetDelegateForFunctionPointer<CNetworkSystemUpdatePublicIp>(funcPtr);
                }
                /*
                struct netadr_t
                {
                uint32_t type
                uint8_t ip[4]
                uint16_t port
                }
                */
                // + 4 to skip type, because the size of uint32_t is 4 bytes
                var ipBytes = (byte*)(_networkSystemUpdatePublicIp(networkSystem) + 4);
                // port is always 0, use the one from convar "hostport"
                return $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.{ipBytes[3]}";
            }
        }

        public static (string, string) GetHostnameAndIp()
        {
            string ip = $"{GetServerIp()}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";
            string hostname = ConVar.Find("hostname")!.StringValue;

            return (hostname, ip);
        }

        public static void PrintToChat(CCSPlayerController player, string message)
        {
            player.PrintToChat($" {Localizer["prefix"]} {message}");
        }

        public static void PrintToChatAll(string message)
        {
            Server.PrintToChatAll($" {Localizer["prefix"]} {message}");
        }
    }
}