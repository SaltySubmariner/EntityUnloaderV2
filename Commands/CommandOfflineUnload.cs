using OfflineUnload.Services;
using Rocket.API;
using System.Collections.Generic;

namespace OfflineUnload.Commands
{
    public class CommandOfflineUnload : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "lo";
        public string Help => "Manually unload a player's saved entities.";
        public string Syntax => "/lo <name|steam64>";
        public List<string> Aliases => new List<string> { "offlineunload" };
        public List<string> Permissions => new List<string> { "offlineunload.unload" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                PlayerResolver.Reply(caller, "Usage: /lo <name|steam64>");
                return;
            }

            string input = string.Join(" ", command);
            if (!PlayerResolver.TryResolve(input, out ulong ownerId, out string displayName))
            {
                PlayerResolver.Reply(caller, $"Could not find online player or Steam64: {input}");
                return;
            }

            int count = OfflineUnloadPlugin.Instance.Service.SaveAndUnload(ownerId, "manual");
            PlayerResolver.Reply(caller, $"Unloaded {count} objects for {displayName} ({ownerId}).");
        }
    }
}
