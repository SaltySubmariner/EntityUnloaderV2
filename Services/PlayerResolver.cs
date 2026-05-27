using Rocket.API;
using SDG.Unturned;
using System;

namespace OfflineUnload.Services
{
    internal static class PlayerResolver
    {
        public static bool TryResolve(string input, out ulong steam64, out string displayName)
        {
            steam64 = 0;
            displayName = input;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (ulong.TryParse(input, out steam64))
            {
                displayName = GetBestKnownName(steam64, input);
                return true;
            }

            string needle = input.Trim().ToLowerInvariant();

            foreach (SteamPlayer client in Provider.clients)
            {
                if (client == null || client.playerID == null)
                    continue;

                string characterName = client.playerID.characterName ?? string.Empty;
                string playerName = client.playerID.playerName ?? string.Empty;
                string nickName = client.playerID.nickName ?? string.Empty;

                if (Matches(characterName, needle) || Matches(playerName, needle) || Matches(nickName, needle))
                {
                    steam64 = client.playerID.steamID.m_SteamID;
                    displayName = !string.IsNullOrWhiteSpace(characterName) ? characterName :
                                  !string.IsNullOrWhiteSpace(playerName) ? playerName :
                                  !string.IsNullOrWhiteSpace(nickName) ? nickName : steam64.ToString();
                    return true;
                }
            }

            return false;
        }

        public static string GetBestKnownName(ulong steam64, string fallback = "Unknown")
        {
            foreach (SteamPlayer client in Provider.clients)
            {
                if (client == null || client.playerID == null || client.playerID.steamID.m_SteamID != steam64)
                    continue;

                string characterName = client.playerID.characterName ?? string.Empty;
                string playerName = client.playerID.playerName ?? string.Empty;
                string nickName = client.playerID.nickName ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(characterName)) return characterName;
                if (!string.IsNullOrWhiteSpace(playerName)) return playerName;
                if (!string.IsNullOrWhiteSpace(nickName)) return nickName;
            }

            return string.IsNullOrWhiteSpace(fallback) ? steam64.ToString() : fallback;
        }

        private static bool Matches(string value, string needle)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string lower = value.ToLowerInvariant();
            return lower == needle || lower.Contains(needle);
        }

        public static void Reply(IRocketPlayer caller, string message)
        {
            if (caller == null || caller.Id == "Console")
            {
                Rocket.Core.Logging.Logger.Log("[OfflineUnload] " + message);
                return;
            }

            if (ulong.TryParse(caller.Id, out ulong callerId))
            {
                Player player = PlayerTool.getPlayer(new Steamworks.CSteamID(callerId));
                if (player != null)
                {
                    ChatManager.serverSendMessage(message, UnityEngine.Color.green, null, player.channel.owner, EChatMode.SAY, null, true);
                    return;
                }
            }

            Rocket.Core.Logging.Logger.Log("[OfflineUnload] " + message);
        }
    }
}
