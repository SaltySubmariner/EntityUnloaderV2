using System;
using System.Linq;
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

            // Steam64 direct
            if (ulong.TryParse(input, out steam64))
            {
                displayName = GetBestKnownName(steam64, input);
                return true;
            }

            var player = Provider.clients
                .Select(c => c?.player)
                .FirstOrDefault(p =>
                {
                    if (p?.channel?.owner?.playerID == null)
                        return false;

                    string charName = p.channel.owner.playerID.characterName ?? "";
                    string nickName = p.channel.owner.playerID.nickName ?? "";

                    return charName.Equals(input, StringComparison.OrdinalIgnoreCase)
                        || nickName.Equals(input, StringComparison.OrdinalIgnoreCase)
                        || charName.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0
                        || nickName.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
                });

            if (player == null)
                return false;

            steam64 = player.channel.owner.playerID.steamID.m_SteamID;

            displayName =
                player.channel.owner.playerID.characterName
                ?? player.channel.owner.playerID.nickName
                ?? steam64.ToString();

            return true;
        }

        public static string GetBestKnownName(ulong steam64, string fallback = null)
        {
            try
            {
                var player = Provider.clients
                    .Select(c => c?.player)
                    .FirstOrDefault(p =>
                        p?.channel?.owner?.playerID?.steamID.m_SteamID == steam64);

                if (player?.channel?.owner?.playerID != null)
                {
                    return player.channel.owner.playerID.characterName
                        ?? player.channel.owner.playerID.nickName
                        ?? steam64.ToString();
                }
            }
            catch
            {
                // ignored intentionally
            }

            return fallback ?? steam64.ToString();
        }

        public static void Reply(IRocketPlayer caller, string message)
        {
            Logger.Log("[OfflineUnload] " + message);
        }
    }
}
