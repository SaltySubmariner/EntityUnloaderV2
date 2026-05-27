using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Unturned.Chat;
using OfflineUnload.Services;

namespace OfflineUnload.Commands
{
    public class CommandOfflineUnload : RocketCommand
    {
        public override AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public override string Name
        {
            get { return "offlineunload"; }
        }

        public override string Help
        {
            get { return "Unload a player's structures, barricades, and vehicles."; }
        }

        public override string Syntax
        {
            get { return "/lo <player/steam64>"; }
        }

        public override List<string> Aliases
        {
            get { return new List<string> { "lo" }; }
        }

        public override List<string> Permissions
        {
            get { return new List<string> { "offlineunload.lo" }; }
        }

        protected override void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Usage: " + Syntax);
                return;
            }

            ulong steam64;
            string displayName;

            if (!PlayerResolver.TryResolve(command[0], out steam64, out displayName))
            {
                UnturnedChat.Say(caller, "Player not found: " + command[0]);
                return;
            }

            int removed = OfflineUnloadPlugin.Instance.Manager.SaveAndUnload(steam64);

            UnturnedChat.Say(
                caller,
                "Unloaded " + removed + " objects for " + displayName + " (" + steam64 + ")."
            );
        }
    }
}
