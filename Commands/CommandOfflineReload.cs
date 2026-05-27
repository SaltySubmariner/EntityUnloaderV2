using OfflineUnload.Services;
using Rocket.API;
using System.Collections.Generic;

namespace OfflineUnload.Commands
{
    public class CommandOfflineReload : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "lr";
        public string Help => "Reload a player's saved entities.";
        public string Syntax => "/lr <name|steam64>";
        public List<string> Aliases => new List<string> { "offlinereload" };
        public List<string> Permissions => new List<string> { "offlineunload.reload" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                PlayerResolver.Reply(caller, "Usage: /lr <name|steam64>");
                return;
            }

            string input = string.Join(" ", command);
            if (!PlayerResolver.TryResolve(input, out ulong ownerId, out string displayName))
            {
                PlayerResolver.Reply(caller, $"Could not find online player or Steam64: {input}");
                return;
            }

            int count = OfflineUnloadPlugin.Instance.Service.Restore(ownerId);
            PlayerResolver.Reply(caller, $"Reloaded {count} objects for {displayName} ({ownerId}).");
        }
    }
}
