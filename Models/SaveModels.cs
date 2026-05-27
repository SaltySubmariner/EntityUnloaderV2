using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OfflineUnload.Models
{
    [DataContract]
    public class OfflineUnloadSave
    {
        [DataMember] public ulong OwnerId { get; set; }
        [DataMember] public string SavedAtUtc { get; set; }
        [DataMember] public string Reason { get; set; }
        [DataMember] public List<SavedStructure> Structures { get; set; } = new List<SavedStructure>();
        [DataMember] public List<SavedBarricade> Barricades { get; set; } = new List<SavedBarricade>();
        [DataMember] public List<SavedVehicle> Vehicles { get; set; } = new List<SavedVehicle>();
    }

    [DataContract]
    public class SavedVector3
    {
        [DataMember] public float X;
        [DataMember] public float Y;
        [DataMember] public float Z;
    }

    [DataContract]
    public class SavedQuaternion
    {
        [DataMember] public float X;
        [DataMember] public float Y;
        [DataMember] public float Z;
        [DataMember] public float W;
    }

    [DataContract]
    public class SavedColor32
    {
        [DataMember] public byte R;
        [DataMember] public byte G;
        [DataMember] public byte B;
        [DataMember] public byte A;
    }

    [DataContract]
    public class SavedItem
    {
        [DataMember] public byte X;
        [DataMember] public byte Y;
        [DataMember] public byte Rot;
        [DataMember] public ushort Id;
        [DataMember] public byte Amount;
        [DataMember] public byte Quality;
        [DataMember] public string StateBase64;
    }

    [DataContract]
    public class SavedStructure
    {
        [DataMember] public string AssetGuid;
        [DataMember] public ushort Health;
        [DataMember] public SavedVector3 Position;
        [DataMember] public SavedQuaternion Rotation;
        [DataMember] public ulong Owner;
        [DataMember] public ulong Group;
        [DataMember] public uint ObjActiveDate;
    }

    [DataContract]
    public class SavedBarricade
    {
        [DataMember] public string AssetGuid;
        [DataMember] public ushort Health;
        [DataMember] public string StateBase64;
        [DataMember] public SavedVector3 Position;
        [DataMember] public SavedQuaternion Rotation;
        [DataMember] public ulong Owner;
        [DataMember] public ulong Group;
        [DataMember] public uint ObjActiveDate;
        [DataMember] public bool IsVehiclePlanted;
        [DataMember] public uint VehicleInstanceId;
        [DataMember] public int SubvehicleIndex;
    }

    [DataContract]
    public class SavedVehicle
    {
        [DataMember] public string AssetGuid;
        [DataMember] public ushort SkinId;
        [DataMember] public ushort MythicId;
        [DataMember] public float RoadPosition;
        [DataMember] public SavedVector3 Position;
        [DataMember] public SavedQuaternion Rotation;
        [DataMember] public bool Sirens;
        [DataMember] public bool Blimp;
        [DataMember] public bool Headlights;
        [DataMember] public bool Taillights;
        [DataMember] public ushort Fuel;
        [DataMember] public ushort Health;
        [DataMember] public ushort BatteryCharge;
        [DataMember] public ulong LockedOwner;
        [DataMember] public ulong LockedGroup;
        [DataMember] public bool Locked;
        [DataMember] public byte TireAliveMask;
        [DataMember] public SavedColor32 PaintColor;
        [DataMember] public bool WasNaturallySpawned;
        [DataMember] public List<SavedItem> TrunkItems { get; set; } = new List<SavedItem>();
        [DataMember] public List<SavedBarricade> AttachedBarricades { get; set; } = new List<SavedBarricade>();
    }
}
