using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private readonly HttpClient client = new HttpClient();
        RecordCache cache = new RecordCache();
        
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetAddonNameDelegate(IntPtr thisPtr);
        
        private string apiUrl = "https://stglobalapi.azurewebsites.net/api";

        public async Task SubmitRecordAsync(object payload)
        {
            if (apiKey == "")
                return;

            if (globalDisabled)
                return;

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Submit", content);

                if (response.IsSuccessStatusCode)
                {
                    Utils.ConPrint("Record submitted successfully.");
                }
                else
                {
                    Utils.LogError($"Failed to submit record. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in SubmitRecordAsync: {ex.Message}");
            }
        }

        public string GetAddonID()
        {
            // https://github.com/alliedmodders/hl2sdk/blob/f3b44f206d38d1b71164e558cd4087d84607d50c/public/iserver.h#L84-L85
            // GetAddonName
            IntPtr networkGameServer = networkServerService.GetIGameServer().Handle;
            IntPtr vtablePtr = Marshal.ReadIntPtr(networkGameServer);
            IntPtr functionPtr = Marshal.ReadIntPtr(vtablePtr + (25 * IntPtr.Size));
            var getAddonName = Marshal.GetDelegateForFunctionPointer<GetAddonNameDelegate>(functionPtr);
            IntPtr result = getAddonName(networkGameServer);
            return Marshal.PtrToStringAnsi(result)!.Split(',')[0]; // return the first id in csv
        }

        public async Task<bool> CheckAddonAsync()
        {
            if (apiKey == "")
                return false;

            try
            {
                var payload = new
                {
                    workshop_id = currentAddonID
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                Utils.LogDebug($"CheckAddon payload: {jsonPayload}");
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/CheckAddonID", content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in CheckAddonAsync: {ex.Message}");
                return false;
            }
        }

        public async Task CacheWorldRecords()
        {
            IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!players.Any())
                return;
            
            var sortedRecords = await GetSortedRecordsFromGlobal(10, 0, currentMapName!, 0);
            cache.CachedWorldRecords = sortedRecords;
        }

        public async Task CacheGlobalPoints()
        {
            IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!players.Any())
                return;
            
            var sortedPoints = await GetTopPointsAsync();
            cache.CachedGlobalPoints = sortedPoints;
        }

        public async Task<List<PlayerPoints>> GetTopPointsAsync(int limit = 10)
        {
            if (apiKey == "")
                return null!;

            try
            {
                var payload = new
                {
                    limit
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/GetPoints", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        var root = jsonDoc.RootElement;
                        var player_points = new List<PlayerPoints>();

                        if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                        {
                            foreach (var playerPoints in dataArray.EnumerateArray())
                            {
                                string playerName = playerPoints.GetProperty("player_name").GetString()!;
                                int points = playerPoints.GetProperty("total_points").GetInt32();
                                player_points.Add(new PlayerPoints
                                {
                                    PlayerName = playerName,
                                    GlobalPoints = points
                                });
                            }
                            return player_points;
                        }
                        return null!;
                    }
                }
                else
                {
                    Utils.LogError($"Failed to get top points. Status code: {response.StatusCode}; Message: {response.Content}");
                    return null!;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetRecordIDAsync: {ex.Message}");
                return null!;
            }
        }

        public async Task<int> GetRecordIDAsync(object payload)
        {
            if (apiKey == "")
                return 0;

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/GetID", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
                        {
                            var recordId = data.GetInt32();
                            return recordId;
                        }
                        else
                        {
                            Utils.LogError($"No record ID found");
                            return 0;
                        }
                    }
                }
                else
                {
                    Utils.LogError($"Failed to retrieve record_id. Status code: {response.StatusCode}; Message: {response.Content}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetRecordIDAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<(int, int, int)> GetGlobalRank(CCSPlayerController player)
        {
            if (apiKey == "")
                return (0, 0, 0);
            
            try
            {
                var payload = new
                {
                    steamid = player!.SteamID
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/GetRank", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                            data.ValueKind != JsonValueKind.Null)
                        {
                            int totalPoints = 0;
                            int rank = 0;
                            int totalPlayers = 0;

                            if (data.TryGetProperty("total_points", out var pointsElement))
                                totalPoints = pointsElement.GetInt32();
                    
                            if (data.TryGetProperty("rank", out var rankElement))
                                rank = rankElement.GetInt32();
                            
                            if (data.TryGetProperty("total_players", out var playersElement))
                                totalPlayers = playersElement.GetInt32();

                            return (totalPoints, rank, totalPlayers);
                        }
                    }
                }
                else
                {
                    Utils.LogError($"Failed to retrieve player rank. Status code: {response.StatusCode}; Message: {response.Content}");
                    return (0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetGlobalRankAsync: {ex.Message}");
                return (0, 0, 0);
            }
            return (0, 0, 0);
        }

        public async Task PrintGlobalRankAsync(CCSPlayerController player)
        {
            if (apiKey == "")
                return;
            
            var (points, rank, totalPlayers) = await GetGlobalRank(player);
            Server.NextFrame(() =>
            {
                Utils.PrintToChat(player, $"{Localizer["total_gpoints"]}: {points}");
                Utils.PrintToChat(player, $"{Localizer["grank"]}: {rank}/{totalPlayers}");
            });
        }

        public async Task SubmitReplayAsync(object payload)
        {
            if (apiKey == "")
                return;

            if (globalDisabled)
                return;

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Replays/Upload", content);

                if (response.IsSuccessStatusCode)
                {
                    Utils.ConPrint("Replay uploaded successfully.");
                }
                else
                {
                    Utils.LogError($"Failed to upload replay. Status code: {response.StatusCode}; Message: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in SubmitReplayAsync: {ex.Message}");
            }
        }

        public void PrintWorldRecord(CCSPlayerController player)
        {
            try
            {
                if (cache.CachedWorldRecords is null)
                    _ = Task.Run(async () => await CacheWorldRecords());
                
                Server.NextFrame(() =>
                {
                    Utils.PrintToChat(player, Localizer["current_wr", currentMapName!]);

                    if (cache.CachedWorldRecords == null || cache.CachedWorldRecords.Count <= 0)
                        return;

                    int position = 1;
                    foreach (var record in cache.CachedWorldRecords)
                    {
                        string replayIndicator = record.Value.Replay ? $"{ChatColors.Red}◉" : "";
                        Utils.PrintToChat(player, $"{Localizer["records_map", position, record.Value.PlayerName!, replayIndicator, Utils.FormatTime(record.Value.TimerTicks)]}");
                        position++;
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in PrintWorldRecord: {ex.Message}");
            }
        }

        public void PrintGlobalPoints(CCSPlayerController player)
        {
            try
            {
                if (cache.CachedGlobalPoints is null)
                    _ = Task.Run(async () => await CacheGlobalPoints());
                
                Server.NextFrame(() =>
                {
                    Utils.PrintToChat(player, Localizer["top_10_points"]);

                    if (cache.CachedGlobalPoints == null || cache.CachedGlobalPoints.Count <= 0)
                        return;

                    int position = 1;
                    foreach (var p in cache.CachedGlobalPoints)
                    {
                        Utils.PrintToChat(player, $"{Localizer["top_10_points_list", position, p.PlayerName!, p.GlobalPoints]}");
                        position++;
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in PrintGlobalPoints: {ex.Message}");
            }
        }

        public async Task<Dictionary<int, PlayerRecord>> GetSortedRecordsFromGlobal(int limit = 0, int bonusX = 0, string mapName = "", int style = 0)
        {
            if (apiKey == "")
                return null!;

            if (globalDisabled)
                return null!;
            
            Utils.LogDebug($"Trying GetSortedRecordsFromGlobal {(bonusX != 0 ? $"bonus {bonusX}" : "")}");
            using (var connection = await OpenConnectionAsync())
            {
                string? currentMapNamee;
                if (string.IsNullOrEmpty(mapName))
                    currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
                else
                    currentMapNamee = mapName;

                var payload = new
                {
                    map_name = currentMapNamee,
                    style,
                    limit
                };

                try
                {
                    var sortedRecords = new Dictionary<int, PlayerRecord>();
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                    HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Sort", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using (var jsonDoc = JsonDocument.Parse(json))
                        {
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                            {
                                int record = 0;
                                foreach (var playerRecord in dataArray.EnumerateArray())
                                {
                                    string playerName = playerRecord.GetProperty("player_name").GetString()!;
                                    int timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                                    string steamId = playerRecord.GetProperty("steamid").GetString()!;
                                    int recordId = playerRecord.GetProperty("record_id").GetInt32();
                                    bool replayExists = playerRecord.GetProperty("replay").GetBoolean();

                                    sortedRecords[record] = new PlayerRecord
                                    {
                                        RecordID = recordId,
                                        SteamID = steamId.ToString(),
                                        PlayerName = playerName,
                                        TimerTicks = timerTicks,
                                        Replay = replayExists
                                    };
                                    record++;
                                }

                                sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                                            .ToDictionary(record => record.Key, record => record.Value);

                                Utils.LogDebug("Got sorted records from global");
                                return sortedRecords;
                            }
                            else
                            {
                                Utils.LogDebug("No data returned");
                                return sortedRecords;
                            }
                        }
                    }
                    else
                    {
                        Utils.LogError($"Failed to GetSortedRecordsFromGlobal. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in GetSortedRecordsFromGlobal: {ex.Message}");
                }
            }
            return [];
        }

        public async Task<string> GetReplayFromGlobal(object payload)
        {
            if (apiKey == "")
                return "";

            if (globalDisabled)
                return "";

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Replays/Download", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Utils.LogError($"Failed to get global replay. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetReplayFromGlobal: {ex.Message}");
            }
            return "";
        }

        public async Task<int> GetPreviousPlayerRecordFromGlobal(string steamId, string currentMapName, string playerName, int bonusX = 0, int style = 0)
        {
            if (apiKey == "")
                return 0;

            if (globalDisabled)
                return 0;
            
            Utils.LogDebug($"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global for {playerName}");
            try
            {
                string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";

                var payload = new
                {
                    steamid = steamId,
                    map_name = currentMapNamee,
                    style
                };
                var sortedRecords = new Dictionary<string, PlayerRecord>();
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/GetPB", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("data", out var dataProperty) && dataProperty.ValueKind == JsonValueKind.Object)
                        {
                            var playerRecord = dataProperty;

                            int recordId = playerRecord.GetProperty("record_id").GetInt32();
                            int timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                            long unixStamp = playerRecord.GetProperty("unix_stamp").GetInt64();

                            return timerTicks;
                        }
                    }
                }
                else
                {
                    Utils.ConPrint($"No previous record found for steamid: {steamId}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global: {ex.Message}");
            }
            return 0;
        }

        public async Task<bool> CheckKeyAsync()
        {
            if (apiKey == "")
                return false;

            if (globalDisabled)
                return false;

            try
            {
                var content = new StringContent("", Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Checks/Key", content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex.Message);
                return false;
            }
        }

        public async Task<bool> CheckHashAsync()
        {
            if (apiKey == "")
                return false;

            if (globalDisabled)
                return false;

            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    Hash = GetHash()
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Checks/Hash", content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                 Utils.LogError(ex.Message);
                return false;
            }
        }

        public (bool, float, float) CheckCvarsAndMaxVelo()
        {
            if (!globalDisabled)
            {
                var equal = Utils.IsApproximatelyEqual;

                if (equal(ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>(), 10)

                && ((equal(ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>(), 150) && currentMapName!.Contains("surf_")) ||
                    (equal(ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>(), 1000) && currentMapName!.Contains("bhop_")))
                
                && equal(ConVar.Find("sv_friction")!.GetPrimitiveValue<float>(), (float)5.2)
                && equal(ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>(), 800)
                && equal(ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>(), 1)
                && equal(ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>(), 0)
                && equal(ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>(), 0)
                && equal(ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>(), 0)
                && equal(ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>(), 0)
                && equal(ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>(), 10)
                && ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>() == false

                && (equal(ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>(), 30) ||
                    equal(ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>(), (float)37.41))
                
                && equal(ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>(), 420)
                && useCheckpointVerification)
                {
                    // THICK
                    globalChecksPassed = true;
                    return (true, ConVar.Find("sv_maxvelocity")!.GetPrimitiveValue<float>(), ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>());
                }

                //Checks failed, disable global api
                Utils.ConPrint($"GLOBAL CHECK FAILED -- Current Values:");
                Utils.ConPrint($"sv_accelerate: {ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>()} [should be 10]");
                Utils.ConPrint($"sv_airaccelerate: {ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>()} [should be 150 for surf_ or 1000 for bhop_]");
                Utils.ConPrint($"sv_friction: {ConVar.Find("sv_friction")!.GetPrimitiveValue<float>()} [should be 5.2]");
                Utils.ConPrint($"sv_gravity: {ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>()} [should be 800]");
                Utils.ConPrint($"sv_ladder_scale_speed: {ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>()} [should be 1]");
                Utils.ConPrint($"sv_staminajumpcost: {ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>()} [should be 0]");
                Utils.ConPrint($"sv_staminalandcost: {ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>()} [should be 0]");
                Utils.ConPrint($"sv_staminamax: {ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>()} [should be 0]");
                Utils.ConPrint($"sv_staminarecoveryrate: {ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>()} [should be 0]");
                Utils.ConPrint($"sv_wateraccelerate: {ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>()} [should be 10]");
                Utils.ConPrint($"sv_maxspeed: {ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>()} [should be 420]");
                Utils.ConPrint($"sharptimer_max_start_speed: {ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>()} [should be 420]");
                Utils.ConPrint($"sv_air_max_wishspeed: {ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>()} [should be 30 or 37.41]");
                Utils.ConPrint($"sv_cheats: {ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>()} [should be false]");
                Utils.ConPrint($"Map is properly zoned?: {useTriggers} [should be true]");
                Utils.ConPrint($"Use checkpoint verification?: {useCheckpointVerification} [should be true]");
                Utils.ConPrint($"Using StripperCS2 on current map?: {Directory.Exists($"{gameDir}/addons/StripperCS2/maps/{Server.MapName}")} [should be false]");

                globalDisabled = true;
                globalChecksPassed = false;
                return (false, 0, 0);
            }
            //Checks failed, disable global api
            return (false, 0, 0);
        }

        public string GetHash()
        {
            string filePath = Path.Join(gameDir, "/csgo/addons/counterstrikesharp/plugins/SharpTimer/SharpTimer.dll");
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return hashString;
                }
            }
        }
    }
}