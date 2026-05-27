using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using OfflineUnload.Services;

namespace OfflineUnload.Commands
{
    public class CommandOfflineReload : RocketCommand
    {
        public override AllowedCaller AllowedCaller => AllowedCaller.Both;

        public override string Name => "offlinereload";

        public override string Help => "Reload a player's saved structures, barricades, and vehicles.";

        public override string Syntax => "/lr <player/steam64>";

        public override List<string> Aliases => new List<string> { "lr" };

        public override List<string> Permissions => new List<string> { "offlineunload.lr" };

        protected override void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, $"Usage: {Syntax}");
                return;
            }

            if (!PlayerResolver.TryResolve(command[0], out ulong steam64, out string displayName))
            {
                UnturnedChat.Say(caller, $"Player not found: {command[0]}");
                return;
            }

            bool success = OfflineUnloadPlugin.Instance.Manager.LoadAndRestore(steam64);

            if (!success)
            {
                UnturnedChat.Say(
                    caller,
                    $"Failed to restore objects for {displayName} ({steam64})."
                );

                return;
            }

            UnturnedChat.Say(
                caller,
                $"Successfully restored objects for {displayName} ({steam64})."
            );
        }
    }
}
