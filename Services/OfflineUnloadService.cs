using OfflineUnload.Models;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace OfflineUnload.Services
{
    public class OfflineUnloadService
    {
        private readonly OfflineUnloadPlugin plugin;
        private string SaveFolder => Path.Combine(plugin.Directory, "Saves");

        public OfflineUnloadService(OfflineUnloadPlugin plugin)
        {
            this.plugin = plugin;
            Directory.CreateDirectory(SaveFolder);
        }

        public string GetSavePath(ulong ownerId) => Path.Combine(SaveFolder, ownerId + ".json");

        public int SaveAndUnload(ulong ownerId, string reason)
        {
            plugin.CurrentlyUnloading.Add(ownerId);

            try
            {
                var path = GetSavePath(ownerId);

                if (File.Exists(path))
                {
                    Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Refused to unload {ownerId}: JSON save already exists. Use /lr first or delete the save manually.");
                    return 0;
                }

                var save = Capture(ownerId, reason);
                int captured = CountSavedObjects(save);

                if (captured <= 0)
                {
                    Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Capture for {ownerId} found 0 objects. Nothing was saved or removed.");
                    return 0;
                }

                Write(save);

                var verify = Read(path);
                int verified = CountSavedObjects(verify);

                if (verified <= 0 || verified != captured)
                {
                    Rocket.Core.Logging.Logger.LogError($"[OfflineUnload] JSON verification failed for {ownerId}. Captured={captured}, Verified={verified}. Nothing was removed.");
                    return 0;
                }

                int removed = RemoveCaptured(save);

                Rocket.Core.Logging.Logger.Log($"[OfflineUnload] Saved {captured} objects to JSON and unloaded {removed} objects for {ownerId}.");
                return removed;
            }
            finally
            {
                plugin.CurrentlyUnloading.Remove(ownerId);
            }
        }

        public int Restore(ulong ownerId)
        {
            var path = GetSavePath(ownerId);
            if (!File.Exists(path))
                return 0;

            var save = Read(path);
            int expected = CountSavedObjects(save);

            if (expected <= 0)
            {
                Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] JSON save for {ownerId} has 0 objects. Refusing to delete save file.");
                return 0;
            }

            int count = RestoreSave(save);

            if (count > 0)
            {
                File.Delete(path);
                Rocket.Core.Logging.Logger.Log($"[OfflineUnload] Restored {count}/{expected} objects for {ownerId} and deleted JSON save.");
            }
            else
            {
                Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Restore for {ownerId} spawned 0 objects. JSON save was NOT deleted.");
            }

            return count;
        }

        public int RestoreNear(Vector3 origin, float radius)
        {
            int count = 0;
            float sqr = radius * radius;

            foreach (var file in Directory.GetFiles(SaveFolder, "*.json"))
            {
                var save = Read(file);

                bool nearby =
                    save.Structures.Any(s => (s.Position.ToUnity() - origin).sqrMagnitude <= sqr) ||
                    save.Barricades.Any(b => !b.IsVehiclePlanted && (b.Position.ToUnity() - origin).sqrMagnitude <= sqr) ||
                    save.Vehicles.Any(v => (v.Position.ToUnity() - origin).sqrMagnitude <= sqr);

                if (!nearby)
                    continue;

                int restored = RestoreSave(save);
                count += restored;

                if (restored > 0)
                    File.Delete(file);
                else
                    Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Radius restore spawned 0 objects from {Path.GetFileName(file)}. JSON save was NOT deleted.");
            }

            return count;
        }

        private OfflineUnloadSave Capture(ulong ownerId, string reason)
        {
            var save = new OfflineUnloadSave
            {
                OwnerId = ownerId,
                Reason = reason,
                SavedAtUtc = DateTime.UtcNow.ToString("o")
            };

            if (plugin.Configuration.Instance.IncludeStructures)
                CaptureStructures(ownerId, save);

            if (plugin.Configuration.Instance.IncludeVehicles)
                CaptureVehicles(ownerId, save);

            if (plugin.Configuration.Instance.IncludeBarricades)
                CaptureWorldBarricades(ownerId, save);

            return save;
        }

        private void CaptureStructures(ulong ownerId, OfflineUnloadSave save)
        {
            var regions = StructureManager.regions;
            if (regions == null)
                return;

            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    var region = regions[x, y];
                    if (region == null)
                        continue;

                    foreach (var drop in region.drops.ToArray())
                    {
                        var data = drop.GetServersideData();

                        if (data == null || data.owner != ownerId || data.structure == null || data.structure.asset == null)
                            continue;

                        if (plugin.Configuration.Instance.BlacklistedStructureIds.Contains(data.structure.asset.id))
                            continue;

                        Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Captured structure {data.structure.asset.id} owner {data.owner}.");

                        save.Structures.Add(new SavedStructure
                        {
                            AssetGuid = data.structure.asset.GUID.ToString(),
                            Health = data.structure.health,
                            Position = data.point.ToSaved(),
                            Rotation = data.rotation.ToSaved(),
                            Owner = data.owner,
                            Group = data.group,
                            ObjActiveDate = data.objActiveDate
                        });
                    }
                }
            }
        }

        private void CaptureWorldBarricades(ulong ownerId, OfflineUnloadSave save)
        {
            var regions = BarricadeManager.regions;
            if (regions == null)
                return;

            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    var region = regions[x, y];
                    if (region == null)
                        continue;

                    foreach (var drop in region.drops.ToArray())
                    {
                        var data = drop.GetServersideData();

                        if (data == null || data.owner != ownerId || data.barricade == null || data.barricade.asset == null)
                            continue;

                        if (plugin.Configuration.Instance.BlacklistedBarricadeIds.Contains(data.barricade.asset.id))
                            continue;

                        RefreshStorageState(drop);
                        Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Captured barricade {data.barricade.asset.id} owner {data.owner}.");
                        save.Barricades.Add(CaptureBarricade(drop, false, 0, 0));
                    }
                }
            }
        }

        private void CaptureVehicles(ulong ownerId, OfflineUnloadSave save)
        {
            foreach (var vehicle in VehicleManager.vehicles.ToArray())
            {
                if (vehicle == null || vehicle.asset == null)
                    continue;

                if (plugin.Configuration.Instance.BlacklistedVehicleIds.Contains(vehicle.asset.id))
                    continue;

                if (plugin.Configuration.Instance.OnlyOwnedLockedVehicles && vehicle.lockedOwner.m_SteamID != ownerId)
                    continue;

                var sv = new SavedVehicle
                {
                    AssetGuid = vehicle.asset.GUID.ToString(),
                    SkinId = vehicle.skinID,
                    MythicId = vehicle.mythicID,
                    RoadPosition = vehicle.roadPosition,
                    Position = vehicle.transform.position.ToSaved(),
                    Rotation = vehicle.transform.rotation.ToSaved(),
                    Sirens = vehicle.sirensOn,
                    Blimp = vehicle.isBlimpFloating,
                    Headlights = vehicle.headlightsOn,
                    Taillights = vehicle.taillightsOn,
                    Fuel = vehicle.fuel,
                    Health = vehicle.health,
                    BatteryCharge = vehicle.batteryCharge,
                    LockedOwner = vehicle.lockedOwner.m_SteamID,
                    LockedGroup = vehicle.lockedGroup.m_SteamID,
                    Locked = vehicle.isLocked,
                    TireAliveMask = vehicle.tireAliveMask,
                    PaintColor = vehicle.PaintColor.ToSaved(),
                    WasNaturallySpawned = vehicle.WasNaturallySpawned
                };

                if (plugin.Configuration.Instance.IncludeNativeVehicleTrunks && vehicle.trunkItems != null)
                    sv.TrunkItems = CaptureItems(vehicle.trunkItems);

                if (plugin.Configuration.Instance.IncludeVehicleBarricades)
                    sv.AttachedBarricades = CaptureAttachedBarricades(vehicle);

                Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Captured vehicle {vehicle.asset.id} owner {vehicle.lockedOwner.m_SteamID}.");
                save.Vehicles.Add(sv);
            }
        }

        private List<SavedBarricade> CaptureAttachedBarricades(InteractableVehicle vehicle)
        {
            var list = new List<SavedBarricade>();

            if (BarricadeManager.vehicleRegions == null)
                return list;

            foreach (var region in BarricadeManager.vehicleRegions)
            {
                if (region == null || region.vehicle != vehicle)
                    continue;

                foreach (var drop in region.drops.ToArray())
                {
                    RefreshStorageState(drop);
                    list.Add(CaptureBarricade(drop, true, vehicle.instanceID, region.subvehicleIndex));
                }
            }

            return list;
        }

        private SavedBarricade CaptureBarricade(BarricadeDrop drop, bool vehiclePlanted, uint vehicleInstanceId, int subvehicleIndex)
        {
            var data = drop.GetServersideData();

            return new SavedBarricade
            {
                AssetGuid = data.barricade.asset.GUID.ToString(),
                Health = data.barricade.health,
                StateBase64 = Convert.ToBase64String(data.barricade.state ?? new byte[0]),
                Position = data.point.ToSaved(),
                Rotation = data.rotation.ToSaved(),
                Owner = data.owner,
                Group = data.group,
                ObjActiveDate = data.objActiveDate,
                IsVehiclePlanted = vehiclePlanted,
                VehicleInstanceId = vehicleInstanceId,
                SubvehicleIndex = subvehicleIndex
            };
        }

        private List<SavedItem> CaptureItems(Items items)
        {
            var result = new List<SavedItem>();

            if (items == null)
                return result;

            for (byte i = 0; i < items.getItemCount(); i++)
            {
                var jar = items.getItem(i);

                if (jar == null || jar.item == null)
                    continue;

                result.Add(new SavedItem
                {
                    X = jar.x,
                    Y = jar.y,
                    Rot = jar.rot,
                    Id = jar.item.id,
                    Amount = jar.item.amount,
                    Quality = jar.item.quality,
                    StateBase64 = Convert.ToBase64String(jar.item.state ?? new byte[0])
                });
            }

            return result;
        }

        private void RefreshStorageState(BarricadeDrop drop)
        {
            if (drop == null || !(drop.interactable is InteractableStorage storage))
                return;

            if (plugin.Configuration.Instance.CloseOpenStorageBeforeUnload && storage.isOpen && storage.opener != null)
            {
                if (storage.opener.inventory != null && storage.opener.inventory.isStoring)
                    storage.opener.inventory.closeStorageAndNotifyClient();

                storage.opener = null;
                storage.isOpen = false;
            }

            storage.rebuildState();
        }

        private void PrepareStorageForUnload(BarricadeDrop drop)
        {
            if (drop == null || !(drop.interactable is InteractableStorage storage))
                return;

            storage.despawnWhenDestroyed = true;

            if (storage.isOpen && storage.opener != null)
            {
                if (storage.opener.inventory != null && storage.opener.inventory.isStoring)
                    storage.opener.inventory.closeStorageAndNotifyClient();

                storage.opener = null;
                storage.isOpen = false;
            }
        }

        private void PrepareVehicleBarricadesForUnload(InteractableVehicle vehicle)
        {
            if (vehicle == null || BarricadeManager.vehicleRegions == null)
                return;

            foreach (var region in BarricadeManager.vehicleRegions)
            {
                if (region == null || region.vehicle != vehicle)
                    continue;

                foreach (var drop in region.drops.ToArray())
                    PrepareStorageForUnload(drop);
            }
        }

        private int RemoveCaptured(OfflineUnloadSave save)
        {
            int count = 0;

            foreach (var v in save.Vehicles)
            {
                var vehicle = VehicleManager.vehicles.FirstOrDefault(x =>
                    x != null &&
                    x.asset != null &&
                    x.lockedOwner.m_SteamID == save.OwnerId &&
                    x.asset.GUID.ToString() == v.AssetGuid &&
                    PositionClose(x.transform.position, v.Position.ToUnity()));

                if (vehicle != null)
                {
                    PrepareVehicleBarricadesForUnload(vehicle);
                    VehicleManager.askVehicleDestroy(vehicle);
                    count++;
                }
            }

            if (BarricadeManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = BarricadeManager.regions[x, y];
                        if (region == null)
                            continue;

                        for (int i = region.drops.Count - 1; i >= 0; i--)
                        {
                            var drop = region.drops[i];
                            var data = drop.GetServersideData();

                            if (data != null && data.owner == save.OwnerId && WasCapturedBarricade(save, data))
                            {
                                PrepareStorageForUnload(drop);
                                BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                                count++;
                            }
                        }
                    }
                }
            }

            if (StructureManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = StructureManager.regions[x, y];
                        if (region == null)
                            continue;

                        for (int i = region.drops.Count - 1; i >= 0; i--)
                        {
                            var drop = region.drops[i];
                            var data = drop.GetServersideData();

                            if (data != null && data.owner == save.OwnerId && WasCapturedStructure(save, data))
                            {
                                StructureManager.destroyStructure(drop, x, y, Vector3.zero, false);
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private bool WasCapturedStructure(OfflineUnloadSave save, StructureData data)
        {
            if (data == null || data.structure == null || data.structure.asset == null)
                return false;

            string guid = data.structure.asset.GUID.ToString();
            return save.Structures.Any(s => s.Owner == data.owner && s.AssetGuid == guid && PositionClose(s.Position.ToUnity(), data.point));
        }

        private bool WasCapturedBarricade(OfflineUnloadSave save, BarricadeData data)
        {
            if (data == null || data.barricade == null || data.barricade.asset == null)
                return false;

            string guid = data.barricade.asset.GUID.ToString();
            return save.Barricades.Any(b => !b.IsVehiclePlanted && b.Owner == data.owner && b.AssetGuid == guid && PositionClose(b.Position.ToUnity(), data.point));
        }

        private bool PositionClose(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude <= 0.04f;
        }

        private int RestoreSave(OfflineUnloadSave save)
        {
            int count = 0;

            foreach (var s in save.Structures)
            {
                if (!Guid.TryParse(s.AssetGuid, out var guid))
                    continue;

                var asset = Assets.find(guid) as ItemStructureAsset;
                if (asset == null)
                    continue;

                StructureManager.dropReplicatedStructure(new Structure(asset, s.Health), s.Position.ToUnity(), s.Rotation.ToUnity(), s.Owner, s.Group);
                count++;
            }

            foreach (var v in save.Vehicles)
            {
                var vehicle = RestoreVehicle(v);

                if (vehicle == null)
                    continue;

                count++;

                foreach (var b in v.AttachedBarricades)
                {
                    if (RestoreBarricade(b, vehicle))
                        count++;
                }
            }

            foreach (var b in save.Barricades)
            {
                if (RestoreBarricade(b, null))
                    count++;
            }

            return count;
        }

        private InteractableVehicle RestoreVehicle(SavedVehicle v)
        {
            if (!Guid.TryParse(v.AssetGuid, out var guid))
                return null;

            var asset = Assets.find(guid) as VehicleAsset;
            if (asset == null)
                return null;

            var vehicle = VehicleManager.SpawnVehicleV3(
                asset,
                v.SkinId,
                v.MythicId,
                v.RoadPosition,
                v.Position.ToUnity(),
                v.Rotation.ToUnity(),
                v.Sirens,
                v.Blimp,
                v.Headlights,
                v.Taillights,
                v.Fuel,
                v.Health,
                v.BatteryCharge,
                new CSteamID(v.LockedOwner),
                new CSteamID(v.LockedGroup),
                v.Locked,
                null,
                v.TireAliveMask,
                v.PaintColor.ToUnity());

            if (vehicle == null)
                return null;

            vehicle.WasNaturallySpawned = v.WasNaturallySpawned;

            if (vehicle.trunkItems != null)
                LoadItems(vehicle.trunkItems, v.TrunkItems);

            return vehicle;
        }

        private bool RestoreBarricade(SavedBarricade b, InteractableVehicle vehicle)
        {
            if (!Guid.TryParse(b.AssetGuid, out var guid))
                return false;

            var asset = Assets.find(guid) as ItemBarricadeAsset;
            if (asset == null)
                return false;

            var state = string.IsNullOrEmpty(b.StateBase64)
                ? new byte[0]
                : Convert.FromBase64String(b.StateBase64);

            var barricade = new Barricade(asset, b.Health, state);

            if (b.IsVehiclePlanted && vehicle != null)
            {
                Transform parent = vehicle.transform;

                if (vehicle.trainCars != null && b.SubvehicleIndex > 0 && b.SubvehicleIndex < vehicle.trainCars.Length)
                    parent = vehicle.trainCars[b.SubvehicleIndex].root;

                return BarricadeManager.dropPlantedBarricade(parent, barricade, b.Position.ToUnity(), b.Rotation.ToUnity(), b.Owner, b.Group) != null;
            }

            return BarricadeManager.dropNonPlantedBarricade(barricade, b.Position.ToUnity(), b.Rotation.ToUnity(), b.Owner, b.Group) != null;
        }

        private void LoadItems(Items items, List<SavedItem> saved)
        {
            if (items == null || saved == null)
                return;

            foreach (var si in saved)
            {
                if (!(Assets.find(EAssetType.ITEM, si.Id) is ItemAsset))
                    continue;

                var state = string.IsNullOrEmpty(si.StateBase64)
                    ? new byte[0]
                    : Convert.FromBase64String(si.StateBase64);

                items.loadItem(si.X, si.Y, si.Rot, new Item(si.Id, si.Amount, si.Quality, state));
            }
        }

        private int CountSavedObjects(OfflineUnloadSave save)
        {
            if (save == null)
                return 0;

            return (save.Structures?.Count ?? 0) +
                   (save.Barricades?.Count ?? 0) +
                   (save.Vehicles?.Count ?? 0);
        }

        private void Write(OfflineUnloadSave save)
        {
            if (CountSavedObjects(save) <= 0)
            {
                Rocket.Core.Logging.Logger.LogWarning($"[OfflineUnload] Refused to write empty JSON save for {save?.OwnerId}.");
                return;
            }

            var path = GetSavePath(save.OwnerId);
            var tempPath = path + ".tmp";
            var serializer = new DataContractJsonSerializer(typeof(OfflineUnloadSave));

            using (var fs = File.Create(tempPath))
                serializer.WriteObject(fs, save);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }

        private OfflineUnloadSave Read(string path)
        {
            var serializer = new DataContractJsonSerializer(typeof(OfflineUnloadSave));

            using (var fs = File.OpenRead(path))
                return (OfflineUnloadSave)serializer.ReadObject(fs);
        }
    }
}
