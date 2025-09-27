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
        RecordCache recordCache = new RecordCache();
        PlayerCache playerCache = new PlayerCache();
        ServerCache serverCache = new ServerCache();
        MapCache mapCache = new MapCache();
        
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetAddonNameDelegate(IntPtr thisPtr);
        
        private string apiUrl = "https://stglobalapi.azurewebsites.net/api";

        public async Task SubmitRecordAsync(object payload)
        {
            if (globalDisabled)
                return;

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Records/Submit", content);

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
        
        public async Task SubmitPointsAsync(int playerId, int points, int recordID)
        {
            if (globalDisabled)
                return;

            try
            {
                var payload = new
                {
                    player_id = playerId,
                    points,
                    record_id = recordID
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Points/SubmitPlayerPoints", content);

                if (response.IsSuccessStatusCode)
                {
                    Utils.ConPrint("Points submitted successfully.");
                }
                else
                {
                    Utils.LogError($"Failed to submit points. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in SubmitPointsAsync: {ex.Message}");
            }
        }

        public async Task UpdatePlayerAsync(long steamid64, string name)
        {
            if (globalDisabled)
                return;
            try
            {
                var payload = new
                {
                    steamid64,
                    name
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Player/UpdatePlayer", content);

                if (response.IsSuccessStatusCode)
                {
                    Utils.LogDebug("Player updated successfully.");
                }
                else
                {
                    Utils.LogError($"Failed to update player. Status code: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Failed to update player: {e.Message}");
            }
        }
        
        public async Task UpdateTotalPointsAsync(int playerId)
        {
            if (globalDisabled)
                return;

            try
            {
                var payload = new
                {
                    player_id = playerId
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Points/UpdateTotalPoints", content);

                if (response.IsSuccessStatusCode)
                {
                    Utils.LogDebug("Total points updated successfully.");
                }
                else
                {
                    Utils.LogError($"Failed to update total points. Status code: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Failed to update total points: {e.Message}");
            }
        }
        
        public async Task<int> GetPlayerIDAsync (long steamid64)
        {
            if (globalDisabled)
                return 0;

            try
            {
                var payload = new
                {
                    steamid64
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Player/GetPlayerID", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
                            return data.GetInt32();
                        
                        Utils.LogError($"No player ID found");
                        return 0;
                    }
                }
                Utils.LogError($"Failed to retrieve player id. Status code: {response.StatusCode}; Message: {response.Content}");
                return 0;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetPlayerIDAsync: {ex.Message}");
                return 0;
            }
        }
        
        public async Task<int> GetServerIDAsync (string ip, int port)
        {
            if (globalDisabled)
                return 0;

            try
            {
                var payload = new
                {
                    ip,
                    port
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Server/GetServerID", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
                            return data.GetInt32();
                        
                        Utils.LogError($"No server ID found");
                        return 0;
                    }
                }
                Utils.LogError($"Failed to retrieve server id. Status code: {response.StatusCode}; Message: {response.Content}");
                return 0;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetServerIDAsync: {ex.Message}");
                return 0;
            }
        }
        
        public async Task<int> GetMapIDAsync (long workshop_id)
        {
            if (globalDisabled)
                return 0;
            
            try
            {
                var payload = new
                {
                    workshop_id
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Map/GetMapID", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
                            return data.GetInt32();
                        
                        Utils.LogError($"No map ID found");
                        return 0;
                    }
                }
                Utils.LogError($"Failed to retrieve map id. Status code: {response.StatusCode}; Message: {response.Content}");
                return 0;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetMapIDAsync: {ex.Message}");
                return 0;
            }
        }

        public long GetAddonID()
        {
            // https://github.com/alliedmodders/hl2sdk/blob/f3b44f206d38d1b71164e558cd4087d84607d50c/public/iserver.h#L84-L85
            // GetAddonName
            IntPtr networkGameServer = networkServerService.GetIGameServer().Handle;
            IntPtr vtablePtr = Marshal.ReadIntPtr(networkGameServer);
            IntPtr functionPtr = Marshal.ReadIntPtr(vtablePtr + (25 * IntPtr.Size));
            var getAddonName = Marshal.GetDelegateForFunctionPointer<GetAddonNameDelegate>(functionPtr);
            IntPtr result = getAddonName(networkGameServer);
            //probably valve map
            if (Marshal.PtrToStringAnsi(result)! == String.Empty)
                return 0;
            return long.Parse(Marshal.PtrToStringAnsi(result)!.Split(',')[0]); // return the first id in csv
        }

        public async Task<bool> CheckAddonAsync(long addonId)
        {
            if (globalDisabled)
                return false;

            try
            {
                var payload = new
                {
                    workshop_id = addonId
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Map/VerifyMap", content);

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

        public void ClearGlobalCache()
        {
            recordCache.CachedStandardWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.Cached85tWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.Cached102tWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.Cached128tWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.CachedSourceWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.CachedBhopWorldRecords = new Dictionary<int, GlobalRecord>();
            recordCache.CachedGlobalPoints = new List<PlayerPoints>();
            
            mapCache.MapID = 0;
            mapCache.AddonID = 0;
            mapCache.MapName = "";
            mapCache.Verified = false;
        }

        public async Task CacheWorldRecords(bool initial = false)
        {
            if (globalDisabled)
                return;
            
            IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!players.Any() && !initial)
                return;
            
            var sortedStandardRecords = await GetSortedRecordsFromGlobal("Normal", "Standard", 0, 10);
            var sorted85tRecords = await GetSortedRecordsFromGlobal("Normal", "85t", 0, 10);
            var sorted102tRecords = await GetSortedRecordsFromGlobal("Normal", "102t", 0, 10);
            var sorted128tRecords = await GetSortedRecordsFromGlobal("Normal", "128t", 0, 10);
            var sortedSourceRecords = await GetSortedRecordsFromGlobal("Normal", "Source", 0, 10);
            var sortedBhopRecords = await GetSortedRecordsFromGlobal("Normal", "Bhop", 0, 10);
            
            recordCache.CachedStandardWorldRecords = sortedStandardRecords;
            recordCache.Cached85tWorldRecords = sorted85tRecords;
            recordCache.Cached102tWorldRecords = sorted102tRecords;
            recordCache.Cached128tWorldRecords = sorted128tRecords;
            recordCache.CachedSourceWorldRecords = sortedSourceRecords;
            recordCache.CachedBhopWorldRecords = sortedBhopRecords;
        }

        public async Task CacheGlobalPoints(bool initial = false)
        {
            if (globalDisabled)
                return;
            
            IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!players.Any() && !initial)
                return;
            
            var sortedPoints = await GetTopPointsAsync();
            recordCache.CachedGlobalPoints = sortedPoints;
        }

        public void CachePlayerID (CCSPlayerController player, int playerId)
        {
            if (globalDisabled)
                return;
            
            playerCache.PlayerID[player.Slot] = playerId;
        }

        public void CacheServerID(int serverId)
        {
            if (globalDisabled)
                return;
            
            serverCache.ServerID = serverId;
            Utils.LogDebug($"Server ID: {serverCache.ServerID}");
        }
        
        public async Task CacheMapData(int mapId, long addonId, string mapName)
        {
            if (globalDisabled)
                return;
            
            mapCache.MapID = mapId;
            mapCache.AddonID = addonId;
            mapCache.MapName = mapName;
            mapCache.Verified = await CheckAddonAsync(addonId);
            Utils.LogDebug("Cached Map Data");
            Utils.LogDebug("Map ID: " + mapCache.MapID);
            Utils.LogDebug("Map Name: " + mapCache.MapName);
            Utils.LogDebug("Addon ID: " + mapCache.AddonID);
            Utils.LogDebug("Verified: " + mapCache.Verified);
        }

        public async Task<List<PlayerPoints>> GetTopPointsAsync(int limit = 10)
        {
            if (globalDisabled)
                return new List<PlayerPoints>();

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

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Points/GetPoints", content);

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

        public async Task<int> GetRecordIDAsync(int playerId, DateTimeOffset createdOn)
        {
            if (globalDisabled)
                return 0;

            try
            {
                var payload = new
                {
                    player_id = playerId,
                    map_id = mapCache.MapID,
                    created_on = createdOn
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Records/GetID", content);

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
                    player_id = playerCache.PlayerID[player.Slot]
                };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Points/GetRank", content);

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
                if (totalPlayers == 0)
                {
                    Utils.PrintToChat(player, $"{Localizer["global_unranked"]}");
                    return;
                }
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
                if (recordCache.CachedStandardWorldRecords is null
                    || recordCache.Cached85tWorldRecords is null
                    || recordCache.Cached102tWorldRecords is null
                    || recordCache.Cached128tWorldRecords is null
                    || recordCache.CachedSourceWorldRecords is null
                    || recordCache.CachedBhopWorldRecords is null)
                    _ = Task.Run(async () => await CacheWorldRecords());
                
                Server.NextFrame(() =>
                {
                    Utils.PrintToChat(player, Localizer["current_wr", currentMapName!]);

                    if (recordCache.CachedStandardWorldRecords is null
                        || recordCache.Cached85tWorldRecords is null
                        || recordCache.Cached102tWorldRecords is null
                        || recordCache.Cached128tWorldRecords is null
                        || recordCache.CachedSourceWorldRecords is null
                        || recordCache.CachedBhopWorldRecords is null)
                        return;

                    int position = 1;
                    Dictionary<int, GlobalRecord> tempCache;
                    switch (playerTimers[player.Slot].Mode)
                    {
                        case "Standard":
                            tempCache = recordCache.CachedStandardWorldRecords;
                            break;
                        case "85t":
                            tempCache = recordCache.Cached85tWorldRecords;
                            break;
                        case "102t":
                            tempCache = recordCache.Cached102tWorldRecords;
                            break;
                        case "128t":
                            tempCache = recordCache.Cached128tWorldRecords;
                            break;
                        case "Source":
                            tempCache = recordCache.CachedSourceWorldRecords;
                            break;
                        case "Bhop":
                            tempCache = recordCache.CachedBhopWorldRecords;
                            break;
                        default:
                            tempCache = recordCache.CachedStandardWorldRecords;
                            break;
                    }
                    
                    foreach (var record in tempCache)
                    {
                        string replayIndicator = record.Value.replay ? $"{ChatColors.Red}◉" : "";
                        Utils.PrintToChat(player, $"{Localizer["records_map", position, record.Value.player_name!, replayIndicator, Utils.FormatDecimalTime(record.Value.time)]}");
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
                if (recordCache.CachedGlobalPoints is null)
                    _ = Task.Run(async () => await CacheGlobalPoints());
                
                Server.NextFrame(() =>
                {
                    Utils.PrintToChat(player, Localizer["top_10_points"]);

                    if (recordCache.CachedGlobalPoints == null || recordCache.CachedGlobalPoints.Count <= 0)
                        return;

                    int position = 1;
                    foreach (var p in recordCache.CachedGlobalPoints)
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

        public async Task<Dictionary<int, GlobalRecord>> GetSortedRecordsFromGlobal(string style = "Normal", string mode = "Standard", int bonus = 0, int limit = 0)
        {
            if (globalDisabled)
                return null!;
            
            Utils.LogDebug($"Trying GetSortedRecordsFromGlobal {(bonus != 0 ? $"bonus {bonus}" : "")}");
            using (var connection = await OpenConnectionAsync())
            {
                var payload = new
                {
                    map_id = mapCache.MapID,
                    style,
                    mode,
                    bonus,
                    limit
                };

                try
                {
                    var sortedRecords = new Dictionary<int, GlobalRecord>();
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                    HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Records/Sort", content);

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
                                    int recordId = playerRecord.GetProperty("record_id").GetInt32();
                                    int playerId = playerRecord.GetProperty("player_id").GetInt32();
                                    string playerName = playerRecord.GetProperty("player_name").GetString()!;
                                    decimal time = playerRecord.GetProperty("time").GetDecimal();
                                    bool replayExists = playerRecord.GetProperty("replay").GetBoolean();

                                    sortedRecords[record] = new GlobalRecord
                                    {
                                        record_id = recordId,
                                        player_id = playerId,
                                        player_name = playerName,
                                        time = time,
                                        replay = replayExists
                                    };
                                    record++;
                                }

                                sortedRecords = sortedRecords.OrderBy(record => record.Value.time)
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

        public async Task<decimal> GetPreviousPlayerRecordFromGlobal(int playerId, string mode = "Standard", string style = "Normal", int bonus = 0)
        {
            if (apiKey == "")
                return 0;

            if (globalDisabled)
                return 0;
            
            Utils.LogDebug($"Trying to get Previous {(bonus != 0 ? $"bonus {bonus} time" : "time")} from global");
            try
            {
                var payload = new
                {
                    player_id = playerId,
                    map_id = mapCache.MapID,
                    mode,
                    style,
                    bonus
                };
                var sortedRecords = new Dictionary<string, GlobalRecord>();
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Records/GetPB", content);

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
                            decimal time = playerRecord.GetProperty("time").GetInt32();
                            DateTimeOffset createdOn = playerRecord.GetProperty("created_on").GetDateTimeOffset();

                            return time;
                        }
                    }
                }
                else
                {
                    Utils.ConPrint($"No previous record found for player");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error getting previous player {(bonus != 0 ? $"bonus {bonus} time" : "time")} from global: {ex.Message}");
            }
            return 0;
        }

        public async Task<bool> CheckKeyAsync()
        {
            if (apiKey == "")
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

        public bool CheckCvars()
        {
            var equal = Utils.IsApproximatelyEqual;

            if (startzoneSingleJumpEnabled
                && useCheckpointVerification
                && useTriggers)
            { 
                return true;
            }
            return false;
        }

        public bool CheckPlugins()
        {
            return !Directory.Exists($"{gameDir}/csgo/addons/StripperCS2/maps/{Server.MapName}") &&
                   File.Exists($"{gameDir}/csgo/addons/metamod/stfixes-metamod.vdf");
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