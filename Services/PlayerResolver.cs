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

            foreach (var client in Provider.clients)
            {
                try
                {
                    if (client == null)
                        continue;

                    var player = client.player;
                    if (player == null)
                        continue;

                    var channel = player.channel;
                    if (channel == null)
                        continue;

                    var owner = channel.owner;
                    if (owner == null)
                        continue;

                    var playerId = owner.playerID;
                    if (playerId == null)
                        continue;

                    string charName = playerId.characterName ?? string.Empty;
                    string nickName = playerId.nickName ?? string.Empty;
                    string playerName = playerId.playerName ?? string.Empty;

                    bool matches =
                        EqualsIgnoreCase(charName, input) ||
                        EqualsIgnoreCase(nickName, input) ||
                        EqualsIgnoreCase(playerName, input) ||
                        ContainsIgnoreCase(charName, input) ||
                        ContainsIgnoreCase(nickName, input) ||
                        ContainsIgnoreCase(playerName, input);

                    if (!matches)
                        continue;

                    steam64 = playerId.steamID.m_SteamID;

                    displayName =
                        !string.IsNullOrWhiteSpace(charName) ? charName :
                        !string.IsNullOrWhiteSpace(nickName) ? nickName :
                        !string.IsNullOrWhiteSpace(playerName) ? playerName :
                        steam64.ToString();

                    return true;
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        public static string GetBestKnownName(ulong steam64, string fallback = null)
        {
            if (Provider.clients == null)
                return fallback ?? steam64.ToString();

            foreach (var client in Provider.clients)
            {
                try
                {
                    if (client == null)
                        continue;

                    var player = client.player;
                    if (player == null)
                        continue;

                    var channel = player.channel;
                    if (channel == null)
                        continue;

                    var owner = channel.owner;
                    if (owner == null)
                        continue;

                    var playerId = owner.playerID;
                    if (playerId == null)
                        continue;

                    if (playerId.steamID.m_SteamID != steam64)
                        continue;

                    string charName = playerId.characterName ?? string.Empty;
                    string nickName = playerId.nickName ?? string.Empty;
                    string playerName = playerId.playerName ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(charName))
                        return charName;

                    if (!string.IsNullOrWhiteSpace(nickName))
                        return nickName;

                    if (!string.IsNullOrWhiteSpace(playerName))
                        return playerName;

                    return steam64.ToString();
                }
                catch
                {
                    continue;
                }
            }

            return fallback ?? steam64.ToString();
        }

        public static void Reply(IRocketPlayer caller, string message)
        {
            Logger.Log("[OfflineUnload] " + message);
        }

        private static bool EqualsIgnoreCase(string a, string b)
        {
            return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsIgnoreCase(string a, string b)
        {
            a = a ?? string.Empty;
            b = b ?? string.Empty;

            if (b.Length == 0)
                return false;

            return a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
