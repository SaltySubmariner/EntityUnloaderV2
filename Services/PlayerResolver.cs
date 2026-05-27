using System;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;

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

            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromName(input);

                if (player != null)
                {
                    steam64 = player.CSteamID.m_SteamID;

                    if (!string.IsNullOrWhiteSpace(player.CharacterName))
                        displayName = player.CharacterName;
                    else if (!string.IsNullOrWhiteSpace(player.DisplayName))
                        displayName = player.DisplayName;
                    else
                        displayName = steam64.ToString();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[OfflineUnload] Rocket name lookup failed: " + ex.Message);
            }

            if (ulong.TryParse(input, out steam64))
            {
                displayName = GetBestKnownName(steam64, input);
                return true;
            }

            return false;
        }

        public static string GetBestKnownName(ulong steam64, string fallback = null)
        {
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new CSteamID(steam64));

                if (player != null)
                {
                    if (!string.IsNullOrWhiteSpace(player.CharacterName))
                        return player.CharacterName;

                    if (!string.IsNullOrWhiteSpace(player.DisplayName))
                        return player.DisplayName;
                }
            }
            catch
            {
            }

            return fallback ?? steam64.ToString();
        }

        public static void Reply(IRocketPlayer caller, string message)
        {
            Logger.Log("[OfflineUnload] " + message);

            try
            {
                if (caller is UnturnedPlayer player)
                {
                    UnturnedChat.Say(player, message);
                }
            }
            catch
            {
            }
        }
    }
}
