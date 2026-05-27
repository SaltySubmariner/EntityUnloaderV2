using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using OfflineUnload.Services;

namespace OfflineUnload.Commands
{
    public class CommandOfflineUnload : RocketCommand
    {
        public override AllowedCaller AllowedCaller => AllowedCaller.Both;

        public override string Name => "offlineunload";

        public override string Help => "Unload a player's structures, barricades, and vehicles.";

        public override string Syntax => "/lo <player/steam64>";

        public override List<string> Aliases => new List<string> { "lo" };

        public override List<string> Permissions => new List<string> { "offlineunload.lo" };

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

            int removed = OfflineUnloadPlugin.Instance.Manager.SaveAndUnload(steam64);

            UnturnedChat.Say(
                caller,
                $"Unloaded {removed} objects for {displayName} ({steam64})."
            );
        }
    }
}
