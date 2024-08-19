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

using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using CounterStrikeSharp.API.Core;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private async Task GetDiscordWebhookURLFromConfigFile(string discordURLpath)
        {
            try
            {
                using JsonDocument? jsonConfig = await LoadJson(discordURLpath)!;
                if (jsonConfig != null)
                {
                    JsonElement root = jsonConfig.RootElement;

                    discordWebhookBotName = root.TryGetProperty("DiscordWebhookBotName", out var NameProperty) ? NameProperty.GetString()! : "SharpTimer";
                    discordWebhookPFPUrl = root.TryGetProperty("DiscordWebhookPFPUrl", out var PFPurlProperty) ? PFPurlProperty.GetString()! : "https://cdn.discordapp.com/icons/1196646791450472488/634963a8207fdb1b30bf909d31f05e57.webp";
                    discordWebhookImageRepoURL = root.TryGetProperty("DiscordWebhookMapImageRepoUrl", out var mapImageRepoUrl) ? mapImageRepoUrl.GetString()! : "https://raw.githubusercontent.com/Letaryat/poor-sharptimermappics/main/pics/";
                    discordPBWebhookUrl = root.TryGetProperty("DiscordPBWebhookUrl", out var PBurlProperty) ? PBurlProperty.GetString()! : "";
                    discordSRWebhookUrl = root.TryGetProperty("DiscordSRWebhookUrl", out var SRurlProperty) ? SRurlProperty.GetString()! : "";
                    discordPBBonusWebhookUrl = root.TryGetProperty("DiscordPBBonusWebhookUrl", out var PBBonusurlProperty) ? PBBonusurlProperty.GetString()! : "";
                    discordSRBonusWebhookUrl = root.TryGetProperty("DiscordSRBonusWebhookUrl", out var SRBonusurlProperty) ? SRBonusurlProperty.GetString()! : "";
                    discordWebhookFooter = root.TryGetProperty("DiscordFooterString", out var FooterProperty) ? FooterProperty.GetString()! : "";
                    discordWebhookRareGif = root.TryGetProperty("DiscordRareGifUrl", out var RareGifProperty) ? RareGifProperty.GetString()! : "";
                    discordWebhookRareGifOdds = root.TryGetProperty("DiscordRareGifOdds", out var RareGifOddsProperty) ? RareGifOddsProperty.GetInt16()! : 10000;
                }
                else
                {
                    SharpTimerError($"DiscordWebhookUrl json was null");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetDiscordWebhookURLFromConfigFile: {ex.Message}");
            }
        }

        public async Task DiscordRecordMessage(CCSPlayerController? player, string playerName, string runTime, string steamID, string placement, int timesFinished, bool isSR = false, string timeDifference = "", int bonusX = 0)
        {
            try
            {
                string? webhookURL = "your_discord_webhook_url";
                if (isSR && bonusX != 0)
                    webhookURL = discordSRBonusWebhookUrl;
                else if (isSR && bonusX == 0)
                    webhookURL = discordSRWebhookUrl;
                else if (!isSR && bonusX != 0)
                    webhookURL = discordPBBonusWebhookUrl;
                else if (!isSR && bonusX == 0)
                    webhookURL = discordPBWebhookUrl;

                if (string.IsNullOrEmpty(webhookURL) || webhookURL == "your_discord_webhook_url")
                {
                    SharpTimerError($"DiscordWebhookUrl was invalid");
                    return;
                }

                string mapImg = await GetMapImage(bonusX);

                bool isFirstTime = string.IsNullOrEmpty(timeDifference);

                using var client = new HttpClient();
                var payload = new
                {
                    content = (string?)null,
                    embeds = new[]
                    {
                        new
                        {
                            title = !isSR ? $"set a new Personal Best!" : $"set a new Server Record!",
                            color = 13369599,
                            fields = new object[]
                            {
                                new
                                {
                                    name = "🗺️ Map:",
                                    value = $"{(bonusX == 0 ? currentMapName : $"{currentMapName} bonus #{bonusX}")}",
                                    inline = true
                                },
                                new
                                {
                                    name = "🔰 Tier:",
                                    value = currentMapTier == null ? 1 : currentMapTier,
                                    inline = true
                                },
                                new
                                {
                                    name = "‎",
                                    value = "‎",
                                    inline = true
                                },
                                new
                                {
                                    name = "⌛ Time:",
                                    value = runTime,
                                    inline = true
                                },
                                new
                                {
                                    name = "⏳ Time change:",
                                    value = $"{(!isFirstTime ? $"{timeDifference}" : "None")}",
                                    inline = true
                                },
                                new
                                {
                                    name = "‎",
                                    value = "‎",
                                    inline = true
                                },
                                new
                                {
                                    name = "🎖️ Placement:",
                                    value = $"#{placement}",
                                    inline = true
                                },
                                new
                                {
                                    name = "🔢 Times Finished:",
                                    value = $"{(!isFirstTime ? timesFinished : "First time!")}",
                                    inline = true
                                },
                                new
                                {
                                    name = "‎",
                                    value = "‎",
                                    inline = true
                                },
                                new
                                {
                                    name = "🛈 SteamID64:",
                                    value = $"[{steamID}](https://steamcommunity.com/profiles/{steamID})",
                                    inline = true
                                },
                                new
                                {
                                    name = "🛹 Style:",
                                    value = $"{GetNamedStyle(playerTimers[player!.Slot].currentStyle)}",
                                    inline = true
                                }
                            },
                            author = new
                            {
                                name = $"{playerName}",
                                url = $"https://steamcommunity.com/profiles/{steamID}"
                            },
                            footer = new
                            {
                                text = discordWebhookFooter,
                                icon_url = discordWebhookPFPUrl
                            },
                            image = new
                            {
                                url = mapImg
                            },
                            thumbnail = new
                            {
                                url = await GetAvatarLink($"https://steamcommunity.com/profiles/{steamID}/?xml=1")
                            }
                        }
                    },
                    username = discordWebhookBotName,
                    avatar_url = discordWebhookPFPUrl,
                    attachments = Array.Empty<object>()
                };

                var json = JsonSerializer.Serialize(payload);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(webhookURL, data);

                if (!response.IsSuccessStatusCode)
                {
                    SharpTimerError($"Failed to send message. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"An error occurred while sending Discord PB message: {ex.Message}");
            }
        }

        public async Task<string> GetMapImage(int bonusX = 0)
        {
            if (new Random().Next(1, discordWebhookRareGifOdds + 1) == 69)
            {
                if (string.IsNullOrEmpty(discordWebhookRareGif))
                    return "https://files.catbox.moe/q99x7v.gif";
                else
                    return discordWebhookRareGif;
            }

            string imageRepo = $"{discordWebhookImageRepoURL}{(bonusX == 0 ? currentMapName : $"{currentMapName}_b{bonusX}")}.jpg";
            string error = $"{discordWebhookImageRepoURL}{(currentMapName!.Contains("surf_") ? "surf404" : $"{(currentMapName!.Contains("kz_") ? "kz404" : $"{(currentMapName!.Contains("bhop_") ? "bhop404" : "404")}")}")}.jpg";
            try
            {
                using var client = new HttpClient();
                if (!await Is404(client, imageRepo))
                {
                    return imageRepo;
                }
                else
                {
                    return error;
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Failed to get DiscordWebhook img. {ex.Message}");
                return error;
            }
        }

        static async Task<bool> Is404(HttpClient client, string url)
        {
            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                return response.StatusCode == HttpStatusCode.NotFound;
            }
            catch (HttpRequestException)
            {
                return true;
            }
        }

        public async Task<string> GetAvatarLink(string xmlUrl)
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(xmlUrl);
                response.EnsureSuccessStatusCode();
                string xmlContent = await response.Content.ReadAsStringAsync();

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                XmlNode? avatarFullNode = xmlDoc.SelectSingleNode("//avatarFull");

                string avatarFullLink = avatarFullNode!.InnerText.Trim();

                return avatarFullLink;
            }
            catch (Exception ex)
            {
                SharpTimerError("GetAvatarLink Error occurred: " + ex.Message);
                return "https://cdn.discordapp.com/icons/1196646791450472488/634963a8207fdb1b30bf909d31f05e57.webp";
            }
        }
    }
}