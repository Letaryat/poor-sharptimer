using System.Data.Common;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private readonly HttpClient client = new HttpClient();
        private string apiUrl = "https://stglobalapi.azurewebsites.net/api";

        public async Task SubmitRecordAsync(object payload)
        {
            if(apiKey == "")
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
                    SharpTimerConPrint("Record submitted successfully.");
                }
                else
                {
                    SharpTimerError($"Failed to submit record. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in SubmitRecordAsync: {ex.Message}");
            }
        }

        public async Task PrintWorldRecordAsync(CCSPlayerController player, object payload)
        {
            try
            {
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
                            var playerRecord = dataArray[0];

                            string playerName = playerRecord.GetProperty("player_name").GetString()!;
                            int timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                            long steamId = playerRecord.GetProperty("steamid").GetInt64();

                            SharpTimerDebug($"Fetched {currentMapName} world record successfully! PlayerName: {playerName}, TimerTicks: {timerTicks}, SteamId: {steamId}");
                            Server.NextFrame(() =>
                            {
                                PrintToChat(player, Localizer["current_wr", currentMapName!]);
                                PrintToChat(player, Localizer["current_wr_player", playerName!, FormatTime(timerTicks)]);
                            });
                        }
                    }
                }
                else
                {
                    SharpTimerError($"Failed to submit record. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in PrintWorldRecordAsync: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, PlayerRecord>> GetSortedRecordsFromGlobal(int limit = 0, int bonusX = 0, string mapName = "", int style = 0)
        {
            
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
                    var sortedRecords = new Dictionary<string, PlayerRecord>();
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    SharpTimerDebug($"GetSortedRecordsFromGlobal payload: {jsonPayload}");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-secret-key", apiKey);

                    HttpResponseMessage response = await client.PostAsync($"{apiUrl}/Sort", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        SharpTimerDebug($"GetSortedRecordsFromGlobal response: {json}");
                        using (var jsonDoc = JsonDocument.Parse(json))
                        {
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                            {
                                foreach (var playerRecord in dataArray.EnumerateArray())
                                {
                                    string playerName = playerRecord.GetProperty("player_name").GetString()!;
                                    int timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                                    long steamId = playerRecord.GetProperty("steamid").GetInt64();

                                    sortedRecords[steamId.ToString()] = new PlayerRecord
                                    {
                                        PlayerName = playerName,
                                        TimerTicks = timerTicks
                                    };
                                }

                                sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                                            .ToDictionary(record => record.Key, record => record.Value);

                                SharpTimerDebug("Got sorted records from global");

                                return sortedRecords;
                            }
                            else
                            {
                                SharpTimerDebug("No data returned");
                                return sortedRecords;
                            }
                        }
                    }
                    else
                    {
                        SharpTimerError($"Failed to GetSortedRecordsFromGlobal. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in GetSortedRecordsFromGlobal: {ex.Message}");
                }
            }
            return [];
        }

        public async Task<int> GetPreviousPlayerRecordFromGlobal(string steamId, string currentMapName, string playerName, int bonusX = 0, int style = 0)
        {
            SharpTimerDebug($"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global for {playerName}");
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

                        if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                        {
                            var playerRecord = dataArray[0];

                            int recordid = playerRecord.GetProperty("record_id").GetInt32();
                            int timerTicks = playerRecord.GetProperty("timer_ticks").GetInt32();
                            string unixStamp = playerRecord.GetProperty("unix_stamp").GetString()!;

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
                SharpTimerError($"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from global: {ex.Message}");
            }
            return 0;
        }

        public (bool, float, float) CheckCvarsAndMaxVelo()
        {
            if (IsApproximatelyEqual(ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>(), 10)
             && IsApproximatelyEqual(ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>(), 150)
             && IsApproximatelyEqual(ConVar.Find("sv_friction")!.GetPrimitiveValue<float>(), (float)5.2)
             && IsApproximatelyEqual(ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>(), 800)
             && IsApproximatelyEqual(ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>(), 1)
             && IsApproximatelyEqual(ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>(), 320)
             && IsApproximatelyEqual(ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>(), 0)
             && IsApproximatelyEqual(ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>(), 0)
             && IsApproximatelyEqual(ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>(), 0)
             && IsApproximatelyEqual(ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>(), 0)
             && IsApproximatelyEqual(ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>(), 10)
             && ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>() == false)
             {
                // THICK
                return (true, ConVar.Find("sv_maxvelocity")!.GetPrimitiveValue<float>(), ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>());
             }
            SharpTimerDebug($"GLOBAL CHECK FAILED");
            SharpTimerDebug($"sv_accelerate: {ConVar.Find("sv_accelerate")!.GetPrimitiveValue<float>()} [should be 10]");
            SharpTimerDebug($"sv_airaccelerate: {ConVar.Find("sv_airaccelerate")!.GetPrimitiveValue<float>()} [should be 150]");
            SharpTimerDebug($"sv_friction: {ConVar.Find("sv_friction")!.GetPrimitiveValue<float>()} [should be 5.2]");
            SharpTimerDebug($"sv_gravity: {ConVar.Find("sv_gravity")!.GetPrimitiveValue<float>()} [should be 800]");
            SharpTimerDebug($"sv_ladder_scale_speed: {ConVar.Find("sv_ladder_scale_speed")!.GetPrimitiveValue<float>()} [should be 1]");
            SharpTimerDebug($"sv_maxspeed: {ConVar.Find("sv_maxspeed")!.GetPrimitiveValue<float>()} [should be 320]");
            SharpTimerDebug($"sv_staminajumpcost: {ConVar.Find("sv_staminajumpcost")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug($"sv_staminalandcost: {ConVar.Find("sv_staminalandcost")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug($"sv_staminamax: {ConVar.Find("sv_staminamax")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug($"sv_staminarecoveryrate: {ConVar.Find("sv_staminarecoveryrate")!.GetPrimitiveValue<float>()} [should be 0]");
            SharpTimerDebug($"sv_wateraccelerate: {ConVar.Find("sv_wateraccelerate")!.GetPrimitiveValue<float>()} [should be 10]");
            SharpTimerDebug($"sv_cheats: {ConVar.Find("sv_cheats")!.GetPrimitiveValue<bool>()} [should be false]");

            return (false, ConVar.Find("sv_maxvelocity")!.GetPrimitiveValue<float>(), ConVar.Find("sv_air_max_wishspeed")!.GetPrimitiveValue<float>());
        }
    }
}