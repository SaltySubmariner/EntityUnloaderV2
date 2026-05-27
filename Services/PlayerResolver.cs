using System;
using Rocket.API;
using Rocket.Core.Logging;
using SDG.Unturned;

namespace OfflineUnload.Services
{
    public static class PlayerResolver
    {
        public static bool TryResolve(string input, out ulong steam64, out string displayName)
        {
            steam64 = 0;
            displayName = input;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            if (ulong.TryParse(input, out steam64))
            {
                displayName = GetBestKnownName(steam64, input);
                return true;
            }

            if (Provider.clients == null)
                return false;

            foreach (SteamPlayer client in Provider.clients)
            {
                if (client == null || client.playerID == null)
                    continue;

                string charName = client.playerID.characterName ?? "";
                string nickName = client.playerID.nickName ?? "";
                string playerName = client.playerID.playerName ?? "";

                if (Matches(charName, input) || Matches(nickName, input) || Matches(playerName, input))
                {
                    steam64 = client.playerID.steamID.m_SteamID;

                    displayName =
                        !string.IsNullOrWhiteSpace(charName) ? charName :
                        !string.IsNullOrWhiteSpace(nickName) ? nickName :
                        !string.IsNullOrWhiteSpace(playerName) ? playerName :
                        steam64.ToString();

                    return true;
                }
            }

            return false;
        }

        public static string GetBestKnownName(ulong steam64, string fallback = null)
        {
            if (Provider.clients != null)
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    if (client == null || client.playerID == null)
                        continue;

                    if (client.playerID.steamID.m_SteamID != steam64)
                        continue;

                    string charName = client.playerID.characterName ?? "";
                    string nickName = client.playerID.nickName ?? "";
                    string playerName = client.playerID.playerName ?? "";

                    if (!string.IsNullOrWhiteSpace(charName))
                        return charName;

                    if (!string.IsNullOrWhiteSpace(nickName))
                        return nickName;

                    if (!string.IsNullOrWhiteSpace(playerName))
                        return playerName;
                }
            }

            return fallback ?? steam64.ToString();
        }

        public static void Reply(IRocketPlayer caller, string message)
        {
            Logger.Log("[OfflineUnload] " + message);
        }

        private static bool Matches(string value, string input)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(input))
                return false;

            return value.Equals(input, StringComparison.OrdinalIgnoreCase)
                || value.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
