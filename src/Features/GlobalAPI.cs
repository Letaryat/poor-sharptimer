using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private readonly HttpClient client = new HttpClient();
        private string apiUrl = "https://rcnoob.club/global/";

        public async Task SubmitRecordAsync(object payload)
        {
            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-Secret-Key", apiKey);

                HttpResponseMessage response = await client.PostAsync($"{apiUrl}/submit.php", content);

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
    }
}