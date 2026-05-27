using OfflineUnload.Services;
using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace OfflineUnload.Commands
{
    public class CommandCheckOwner : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ck";
        public string Help => "Check who owns the structure, barricade, storage, or vehicle you are looking at.";
        public string Syntax => "/ck";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "offlineunload.ck" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!ulong.TryParse(caller.Id, out ulong callerId))
                return;

            Player player = PlayerTool.getPlayer(new CSteamID(callerId));
            if (player == null)
                return;

            Ray ray = new Ray(player.look.aim.position, player.look.aim.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 12f))
            {
                Send(player, "You are not looking at a structure, barricade, storage, or vehicle.");
                return;
            }

            if (TryGetVehicle(hit.transform, out InteractableVehicle vehicle))
            {
                ulong owner = vehicle.lockedOwner.m_SteamID;
                string itemName = vehicle.asset != null ? vehicle.asset.vehicleName : "vehicle";
                SendOwner(player, owner, itemName);
                return;
            }

            if (TryGetBarricade(hit.transform, out BarricadeData barricadeData))
            {
                ulong owner = barricadeData.owner;
                string itemName = barricadeData.barricade != null && barricadeData.barricade.asset != null
                    ? barricadeData.barricade.asset.itemName
                    : "barricade";
                SendOwner(player, owner, itemName);
                return;
            }

            if (TryGetStructure(hit.transform, out StructureData structureData))
            {
                ulong owner = structureData.owner;
                string itemName = structureData.structure != null && structureData.structure.asset != null
                    ? structureData.structure.asset.itemName
                    : "structure";
                SendOwner(player, owner, itemName);
                return;
            }

            Send(player, "You are not looking at a structure, barricade, storage, or vehicle.");
        }

        private bool TryGetVehicle(Transform hit, out InteractableVehicle vehicle)
        {
            vehicle = hit.GetComponentInParent<InteractableVehicle>();
            return vehicle != null;
        }

        private bool TryGetBarricade(Transform hit, out BarricadeData data)
        {
            data = null;

            if (BarricadeManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = BarricadeManager.regions[x, y];
                        if (region == null)
                            continue;

                        foreach (var drop in region.drops)
                        {
                            if (drop == null || drop.model == null)
                                continue;

                            if (hit == drop.model || hit.IsChildOf(drop.model))
                            {
                                data = drop.GetServersideData();
                                return data != null;
                            }
                        }
                    }
                }
            }

            if (BarricadeManager.vehicleRegions != null)
            {
                foreach (var region in BarricadeManager.vehicleRegions)
                {
                    if (region == null)
                        continue;

                    foreach (var drop in region.drops)
                    {
                        if (drop == null || drop.model == null)
                            continue;

                        if (hit == drop.model || hit.IsChildOf(drop.model))
                        {
                            data = drop.GetServersideData();
                            return data != null;
                        }
                    }
                }
            }

            return false;
        }

        private bool TryGetStructure(Transform hit, out StructureData data)
        {
            data = null;

            if (StructureManager.regions == null)
                return false;

            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    var region = StructureManager.regions[x, y];
                    if (region == null)
                        continue;

                    foreach (var drop in region.drops)
                    {
                        if (drop == null || drop.model == null)
                            continue;

                        if (hit == drop.model || hit.IsChildOf(drop.model))
                        {
                            data = drop.GetServersideData();
                            return data != null;
                        }
                    }
                }
            }

            return false;
        }

        private void SendOwner(Player player, ulong owner, string itemName)
        {
            string name = owner == 0 ? "No owner" : PlayerResolver.GetBestKnownName(owner, "Unknown");
            Send(player, $"{name} ({owner}) owns this {itemName}.");
        }

        private void Send(Player player, string message)
        {
            ChatManager.serverSendMessage(message, Color.yellow, null, player.channel.owner, EChatMode.SAY, null, true);
        }
    }
}
