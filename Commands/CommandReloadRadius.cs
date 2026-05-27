using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace OfflineUnload.Commands
{
    public class CommandReloadRadius : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "lradius";
        public string Help => "Reload saved objects near you.";
        public string Syntax => "/lradius <radius>";
        public List<string> Aliases => new List<string> { "lrad" };
        public List<string> Permissions => new List<string> { "offlineunload.lradius" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            float radius = 50f;
            if (command.Length > 0)
                float.TryParse(command[0], out radius);

            ulong steamId;
            if (!ulong.TryParse(caller.Id, out steamId))
                return;

            Player player = PlayerTool.getPlayer(new CSteamID(steamId));
            if (player == null)
                return;

            int count = OfflineUnloadPlugin.Instance.Service.RestoreNear(player.transform.position, radius);
            ChatManager.serverSendMessage($"Reloaded {count} saved objects within {radius}m.", Color.green, null, player.channel.owner, EChatMode.SAY, null, true);
        }
    }
}
