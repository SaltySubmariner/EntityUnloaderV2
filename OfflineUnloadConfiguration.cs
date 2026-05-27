using Rocket.API;
using System.Collections.Generic;

namespace OfflineUnload
{
    public class OfflineUnloadConfiguration : IRocketPluginConfiguration
    {
        public bool AutoUnloadOnDisconnect { get; set; }
        public float AutoUnloadDelaySeconds { get; set; }
        public bool IncludeStructures { get; set; }
        public bool IncludeBarricades { get; set; }
        public bool IncludeVehicles { get; set; }
        public bool IncludeVehicleBarricades { get; set; }
        public bool IncludeNativeVehicleTrunks { get; set; }
        public bool CloseOpenStorageBeforeUnload { get; set; }
        public bool DenyStorageOpenWhileUnloading { get; set; }
        public bool OnlyOwnedLockedVehicles { get; set; }
        public List<ushort> BlacklistedStructureIds { get; set; }
        public List<ushort> BlacklistedBarricadeIds { get; set; }
        public List<ushort> BlacklistedVehicleIds { get; set; }

        public void LoadDefaults()
        {
            AutoUnloadOnDisconnect = true;
            AutoUnloadDelaySeconds = 0.25f;
            IncludeStructures = true;
            IncludeBarricades = true;
            IncludeVehicles = true;
            IncludeVehicleBarricades = true;
            IncludeNativeVehicleTrunks = true;
            CloseOpenStorageBeforeUnload = true;
            DenyStorageOpenWhileUnloading = true;
            OnlyOwnedLockedVehicles = true;
            BlacklistedStructureIds = new List<ushort>();
            BlacklistedBarricadeIds = new List<ushort>();
            BlacklistedVehicleIds = new List<ushort>();
        }
    }
}
