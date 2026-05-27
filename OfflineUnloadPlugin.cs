using OfflineUnload.Services;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OfflineUnload
{
    public class OfflineUnloadPlugin : RocketPlugin<OfflineUnloadConfiguration>
    {
        public static OfflineUnloadPlugin Instance { get; private set; }
        public OfflineUnloadService Service { get; private set; }

        internal readonly HashSet<ulong> CurrentlyUnloading = new HashSet<ulong>();

        protected override void Load()
        {
            Instance = this;
            Service = new OfflineUnloadService(this);

            Provider.onServerConnected += OnServerConnected;
            Provider.onServerDisconnected += OnServerDisconnected;
            BarricadeManager.onOpenStorageRequested += OnOpenStorageRequested;

            Rocket.Core.Logging.Logger.Log("[OfflineUnload] Loaded.");
        }

        protected override void Unload()
        {
            BarricadeManager.onOpenStorageRequested -= OnOpenStorageRequested;
            Provider.onServerDisconnected -= OnServerDisconnected;
            Provider.onServerConnected -= OnServerConnected;

            Service = null;
            Instance = null;

            Rocket.Core.Logging.Logger.Log("[OfflineUnload] Unloaded.");
        }

        private void OnServerConnected(CSteamID playerId)
        {
            StartCoroutine(RestoreAfterDelay(playerId));
        }

        private IEnumerator RestoreAfterDelay(CSteamID playerId)
        {
            yield return new WaitForSeconds(2f);
            Service.Restore(playerId.m_SteamID);
        }

        private void OnServerDisconnected(CSteamID playerId)
        {
            if (!Configuration.Instance.AutoUnloadOnDisconnect)
                return;

            StartCoroutine(UnloadAfterDelay(playerId));
        }

        private IEnumerator UnloadAfterDelay(CSteamID playerId)
        {
            yield return new WaitForSeconds(Configuration.Instance.AutoUnloadDelaySeconds);
            Service.SaveAndUnload(playerId.m_SteamID, "disconnect");
        }

        private void OnOpenStorageRequested(CSteamID playerId, InteractableStorage storage, ref bool shouldAllow)
        {
            if (!Configuration.Instance.DenyStorageOpenWhileUnloading || storage == null)
                return;

            if (CurrentlyUnloading.Contains(storage.owner.m_SteamID))
                shouldAllow = false;
        }
    }
}
