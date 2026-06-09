using OfflineUnload.Models;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Player;
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
            if (ShouldBypassUnload(ownerId))
            {
                Rocket.Core.Logging.Logger.Log($"[OfflineUnload] Skipped unload for protected player {ownerId}.");
                return 0;
            }

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
                Rocket.Core.Logging.Logger.Log("[OfflineUnload] Restored " + count + "/" + expected + " objects for " + ownerId + " and deleted JSON save.");
