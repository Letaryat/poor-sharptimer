using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer;

public partial class SharpTimer
{
    private readonly string apiUrl = "https://stglobalapi.azurewebsites.net/api";
    private readonly RecordCache cache = new();
    private readonly HttpClient client = new();

    private async Task SubmitRecordAsync(object payload)
    {
        if (apiKey == "")
            return;

        if (globalDisabled)
            return;

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/Submit", content);

            if (response.IsSuccessStatusCode)
                SharpTimerConPrint("Record submitted successfully.");
            else
                SharpTimerError($"Failed to submit record. Status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in SubmitRecordAsync: {ex.Message}");
        }
    }

    private async Task CacheWorldRecords()
    {
        var sortedRecords = await GetSortedRecordsFromGlobal(10, 0, currentMapName!);

        cache.CachedWorldRecords = sortedRecords;
    }

    private async Task CacheGlobalPoints()
    {
        var sortedPoints = await GetTopPointsAsync();

        cache.CachedGlobalPoints = sortedPoints;
    }

    private async Task<List<PlayerPoints>> GetTopPointsAsync(int limit = 10)
    {
        if (apiKey == "")
            return null;

        try
        {
            var payload = new
            {
                limit
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/GetPoints", content);

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
                            var playerName = playerPoints.GetProperty("player_name").GetString()!;
                            var points = playerPoints.GetProperty("total_points").GetInt32();
                            player_points.Add(new PlayerPoints
                            {
                                PlayerName = playerName,
                                GlobalPoints = points
                            });
                        }

                        return player_points;
                    }

                    return null;
                }
            }

            SharpTimerError(
                $"Failed to get top points. Status code: {response.StatusCode}; Message: {response.Content}");
            return null;
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in GetRecordIDAsync: {ex.Message}");
            return null;
        }
    }

    private async Task<int> GetRecordIDAsync(object payload)
    {
        if (apiKey == "")
            return 0;

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/GetID", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using (var jsonDoc = JsonDocument.Parse(json))
                {
                    if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                        data.ValueKind != JsonValueKind.Null)
                    {
                        var recordId = data.GetInt32();
                        return recordId;
                    }

                    SharpTimerError("No record ID found");
                    return 0;
                }
            }

            SharpTimerError(
                $"Failed to retrieve record_id. Status code: {response.StatusCode}; Message: {response.Content}");
            return 0;
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in GetRecordIDAsync: {ex.Message}");
            return 0;
        }
    }

    private async Task SubmitReplayAsync(object payload)
    {
        if (apiKey == "")
            return;

        if (globalDisabled)
            return;

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/Replays/Upload", content);

            if (response.IsSuccessStatusCode)
                SharpTimerConPrint("Replay uploaded successfully.");
            else
                SharpTimerError(
                    $"Failed to upload replay. Status code: {response.StatusCode}; Message: {response.Content}");
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in SubmitReplayAsync: {ex.Message}");
        }
    }

    private void PrintWorldRecord(CCSPlayerController player)
    {
        try
        {
            Server.NextFrame(() =>
            {
                PrintToChat(player, Localizer["current_wr", currentMapName!]);
                var position = 1;
                foreach (var record in cache.CachedWorldRecords!)
                {
                    var replayIndicator = record.Value.Replay ? $"{ChatColors.Red}◉" : "";
                    PrintToChat(player,
                        $"{Localizer["records_map", position, record.Value.PlayerName!, replayIndicator, FormatTime(record.Value.TimerTicks)]}");
                    position++;
                }
            });
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in PrintWorldRecord: {ex.Message}");
        }
    }

    private void PrintGlobalPoints(CCSPlayerController player)
    {
        try
        {
            Server.NextFrame(() =>
            {
                PrintToChat(player, Localizer["top_10_points"]);
                var position = 1;
                foreach (var p in cache.CachedGlobalPoints!)
                {
                    PrintToChat(player, $"{Localizer["top_10_points_list", position, p.PlayerName!, p.GlobalPoints]}");
                    position++;
                }
            });
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in PrintGlobalPoints: {ex.Message}");
        }
    }

    private async Task<Dictionary<int, PlayerRecord>> GetSortedRecordsFromGlobal(int limit = 0, int bonusX = 0,
        string mapName = "", int style = 0)
    {
        if (apiKey == "")
            return null;

        if (globalDisabled)
            return null;

        SharpTimerDebug($"Trying GetSortedRecordsFromGlobal {(bonusX != 0 ? $"bonus {bonusX}" : "")}");
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
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                SharpTimerDebug($"GetSortedRecordsFromGlobal payload: {jsonPayload}");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                var response = await client.PostAsync($"{apiUrl}/Sort", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    SharpTimerDebug($"GetSortedRecordsFromGlobal response: {json}");
                    using (var jsonDoc = JsonDocument.Parse(json))
                    {
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                        {
                            var record = 0;
                            foreach (var playerRecord in dataArray.EnumerateArray())
                            {
                                var playerName = playerRecord.GetProperty("player_name").GetString()!;
                                var timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                                var steamId = playerRecord.GetProperty("steamid").GetInt64();
                                var recordId = playerRecord.GetProperty("record_id").GetInt32();
                                var replayExists = playerRecord.GetProperty("replay").GetBoolean();

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

                            SharpTimerDebug("Got sorted records from global");

                            return sortedRecords;
                        }

                        SharpTimerDebug("No data returned");
                        return sortedRecords;
                    }
                }

                SharpTimerError($"Failed to GetSortedRecordsFromGlobal. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetSortedRecordsFromGlobal: {ex.Message}");
            }
        }

        return [];
    }

    private async Task<string> GetReplayFromGlobal(object payload)
    {
        if (apiKey == "")
            return "";

        if (globalDisabled)
            return "";

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/Replays/Download", content);

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();

            SharpTimerError($"Failed to get global replay. Status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            SharpTimerError($"Error in GetReplayFromGlobal: {ex.Message}");
        }

        return "";
    }

    private async Task<int> GetPreviousPlayerRecordFromGlobal(string steamId, string currentMapName, string playerName,
        int bonusX = 0, int style = 0)
    {
        if (apiKey == "")
            return 0;

        if (globalDisabled)
            return 0;

        SharpTimerDebug(
            $"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global for {playerName}");
        try
        {
            var currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";

            var payload = new
            {
                steamid = steamId,
                map_name = currentMapNamee,
                style
            };
            var sortedRecords = new Dictionary<string, PlayerRecord>();
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

            var response = await client.PostAsync($"{apiUrl}/GetPB", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using (var jsonDoc = JsonDocument.Parse(json))
                {
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("data", out var dataProperty) &&
                        dataProperty.ValueKind == JsonValueKind.Object)
                    {
                        var playerRecord = dataProperty;

                        var recordId = playerRecord.GetProperty("record_id").GetInt32();
                        var timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                        var unixStamp = playerRecord.GetProperty("unix_stamp").GetInt64();

                        return timerTicks;
                    }
                }
            }
            else
            {
                SharpTimerConPrint($"No previous record found for steamid: {steamId}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            SharpTimerError(
                $"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global: {ex.Message}");
        }

        return 0;
    }

    private async Task<bool> CheckKeyAsync()
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

            var response = await client.PostAsync($"{apiUrl}/Checks/Key", content);

            if (response.IsSuccessStatusCode) return true;

            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<bool> CheckHashAsync()
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

            var response = await client.PostAsync($"{apiUrl}/Checks/Hash", content);

            if (response.IsSuccessStatusCode) return true;

            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private (bool, float, float) CheckCvarsAndMaxVelo()
    {
        if (!globalDisabled)
        {
            if (IsApproximatelyEqual(ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>(), 10)
                && ((IsApproximatelyEqual(ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>(), 150) &&
                     currentMapName!.Contains("surf_"))
                    || (IsApproximatelyEqual(ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>(), 1000) &&
                        currentMapName!.Contains("bhop_")))
                && IsApproximatelyEqual(ConVar.Find("sv_friction")!.GetPrimitiveValue<float>(), (float)5.2)
                && IsApproximatelyEqual(ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>(), 800)
                && IsApproximatelyEqual(ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>(), 1)
                && IsApproximatelyEqual(ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>(), 0)
                && IsApproximatelyEqual(ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>(), 0)
                && IsApproximatelyEqual(ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>(), 0)
                && IsApproximatelyEqual(ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>(), 0)
                && IsApproximatelyEqual(ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>(), 10)
                && ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>() == false
                && (IsApproximatelyEqual(ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>(), 30) ||
                    IsApproximatelyEqual(ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>(), (float)37.41))
                && IsApproximatelyEqual(ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>(), 420))
            {
                // THICK
                globalChecksPassed = true;
                return (true, ConVar.Find("sv_maxvelocity")!.GetPrimitiveValue<float>(),
                    ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>());
            }

            //Checks failed, disable global api
            SharpTimerDebug("GLOBAL CHECK FAILED -- Current Values:");
            SharpTimerDebug(
                $"sv_accelerate: {ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>()} [should be 10]");
            SharpTimerDebug(
                $"sv_airaccelerate: {ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>()} [should be 150 for surf_ or 1000 for bhop_]");
            SharpTimerDebug($"sv_friction: {ConVar.Find("sv_friction")!.GetPrimitiveValue<float>()} [should be 5.2]");
            SharpTimerDebug($"sv_gravity: {ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>()} [should be 800]");
            SharpTimerDebug(
                $"sv_ladder_scale_speed: {ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>()} [should be 1]");
            SharpTimerDebug(
                $"sv_staminajumpcost: {ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug(
                $"sv_staminalandcost: {ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug($"sv_staminamax: {ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug(
                $"sv_staminarecoveryrate: {ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug(
                $"sv_wateraccelerate: {ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>()} [should be 10]");
            SharpTimerDebug($"sv_maxspeed: {ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>()} [should be 420]");
            SharpTimerDebug(
                $"sharptimer_max_start_speed: {ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>()} [should be 420]");
            SharpTimerDebug(
                $"sv_air_max_wishspeed: {ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>()} [should be 30 or 37.41]");
            SharpTimerDebug($"sv_cheats: {ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>()} [should be false]");
            SharpTimerDebug($"Map is properly zoned?: {useTriggers} [should be true]");

            globalDisabled = true;
            globalChecksPassed = false;
            return (false, 0, 0);
        }

        //Checks failed, disable global api
        return (false, 0, 0);
    }

    private string GetHash()
    {
        var filePath = Path.Join(gameDir, "/csgo/addons/counterstrikesharp/plugins/SharpTimer/SharpTimer.dll");
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