# OfflineUnload

RocketMod plugin for Unturned that saves and unloads a player's owned structures, barricades, storages, vehicles, vehicle trunks, vehicle-planted barricades, crops, tanks, generators, displays, and mannequins when they disconnect.

## Commands

- `/lo <player>` - unload an offline/online player's owned world objects.
- `/lr <player>` - reload that player's saved world objects.
- `/lradius <radius>` - reload saved objects near you.

Aliases:

- `/lo` = load out / offline unload
- `/lr` = load restore

## Permissions

- `offlineunload.lo`
- `offlineunload.lr`
- `offlineunload.lradius`

## Build paths

This project expects these MSBuild properties:

- `UnturnedManaged` = path to Unturned Dedicated Server `Unturned_Data/Managed`
- `RocketLibraries` = path to RocketMod `Libraries`

Example local build:

```bash
dotnet build -p:UnturnedManaged="C:\Servers\Unturned\Unturned_Data\Managed" -p:RocketLibraries="C:\Servers\Unturned\Servers\YOURSERVER\Rocket\Libraries"
```

## GitHub Actions

Set repository secrets:

- `UNTURNED_MANAGED_ZIP_URL` - private URL to a zip containing Assembly-CSharp.dll, UnityEngine modules, Steamworks.NET.dll, etc.
- `ROCKET_LIBRARIES_ZIP_URL` - private URL to a zip containing Rocket.API.dll, Rocket.Core.dll, Rocket.Unturned.dll.
