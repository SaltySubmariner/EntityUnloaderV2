using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Unturned.Chat;
using OfflineUnload.Services;

namespace OfflineUnload.Commands
{
    public class CommandOfflineReload : RocketCommand
    {
        public override AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public override string Name
        {
            get { return "offlinereload"; }
        }

        public override string Help
        {
            get { return "Reload a player's saved structures, barricades, and vehicles."; }
        }

        public override string Syntax
        {
            get { return "/lr <player/steam64>"; }
        }

        public override List<string> Aliases
        {
            get { return new List<string> { "lr" }; }
        }

        public override List<string> Permissions
        {
            get { return new List<string> { "offlineunload.lr" }; }
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

            bool success = OfflineUnloadPlugin.Instance.Manager.LoadAndRestore(steam64);

            if (!success)
            {
                UnturnedChat.Say(
                    caller,
                    "Failed to restore objects for " + displayName + " (" + steam64 + ")."
                );

                return;
            }

            UnturnedChat.Say(
                caller,
                "Successfully restored objects for " + displayName + " (" + steam64 + ")."
            );
        }
    }
}
