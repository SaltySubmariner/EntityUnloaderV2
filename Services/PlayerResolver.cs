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

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                input = input.Trim();

                // Steam64 direct
                if (ulong.TryParse(input, out steam64))
                {
                    displayName = GetBestKnownName(steam64, input);
                    return true;
                }

                if (Provider.clients == null)
                    return false;

                foreach (SteamPlayer client in Provider.clients)
                {
                    try
                    {
                        if (client == null)
                            continue;

                        var id = client.playerID;

                        if (id == null)
                            continue;

                        ulong clientSteam64 = 0;

                        try
                        {
                            clientSteam64 = id.steamID.m_SteamID;
                        }
                        catch
                        {
                            continue;
                        }

                        string charName = "";
                        string nickName = "";
                        string playerName = "";

                        try { charName = id.characterName ?? ""; } catch { }
                        try { nickName = id.nickName ?? ""; } catch { }
                        try { playerName = id.playerName ?? ""; } catch { }

                        bool matched =
                            Matches(charName, input) ||
                            Matches(nickName, input) ||
                            Matches(playerName, input);

                        if (!matched)
                            continue;

                        steam64 = clientSteam64;

                        if (!string.IsNullOrWhiteSpace(charName))
                            displayName = charName;
                        else if (!string.IsNullOrWhiteSpace(nickName))
                            displayName = nickName;
                        else if (!string.IsNullOrWhiteSpace(playerName))
                            displayName = playerName;
                        else
                            displayName = steam64.ToString();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("[OfflineUnload] Failed checking one player: " + ex.Message);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError("[OfflineUnload] PlayerResolver.TryResolve failed: " + ex);
                return false;
            }
        }

        public static string GetBestKnownName(ulong steam64, string fallback = null)
        {
            try
            {
                if (Provider.clients == null)
                    return fallback ?? steam64.ToString();

                foreach (SteamPlayer client in Provider.clients)
                {
                    try
                    {
                        if (client == null)
                            continue;

                        var id = client.playerID;

                        if (id == null)
                            continue;

                        ulong clientSteam64 = 0;

                        try
                        {
                            clientSteam64 = id.steamID.m_SteamID;
                        }
                        catch
                        {
                            continue;
                        }

                        if (clientSteam64 != steam64)
                            continue;

                        string charName = "";
                        string nickName = "";
                        string playerName = "";

                        try { charName = id.characterName ?? ""; } catch { }
                        try { nickName = id.nickName ?? ""; } catch { }
                        try { playerName = id.playerName ?? ""; } catch { }

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
            }
            catch
            {
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
