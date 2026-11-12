# FM26 Custom Kits

BepInEx/Harmony plugin for Football Manager 26 that injects custom kit textures into 3D match rendering and 2D UI screens, replaces abbreviated Brazilian club names with full names, and enables facepacks — all without modifying any game files.

> **Guia completo em português:** https://claude.ai/artifact/XpwnSe9zoaeYd1V1NRYkTv

---

## How it works

FM26 uses Unity IL2CPP with an addressable asset system. The old `graphics/kits/` folder no longer works. This plugin intercepts the engine's internal rendering methods at runtime:

- `GraphicsUtils.ResolveKitResourceID` — identifies which team is being set up
- `KitMaterials.ApplyGenericKitToTextureArray` — intercepts generic kit rendering, replaces with custom PNG via `ApplyLicensedKitWithNumberToTextureArray`
- `DatabaseObjectImageHelpers.ConvertDynamicReferenceToAddressableID` — replaces 2D kit in UI screens
- `TextElement.text` setter — replaces abbreviated club names with full names

## Requirements

- Football Manager 26 (Steam)
- [BepInEx 6 Bleeding Edge IL2CPP x64](https://builds.bepinex.dev/projects/bepinex_be)
- .NET SDK 8+ (to compile from source)

## Installation

1. Install BepInEx 6 IL2CPP in the FM26 folder and run the game once
2. Download the latest release and extract to `<FM26>/BepInEx/plugins/`
3. Run the game — the plugin creates the folders on first launch

```
<FM26>/BepInEx/plugins/FM26CustomKits/
├── FM26CustomKits.dll
├── nomes.txt        ← club name replacements
├── kits3d/          ← <ID>_home.png, <ID>_away.png, <ID>_third.png, <ID>_gk.png (1024×1024)
└── kits2d/          ← <signature>.png (256×256, transparent background)
```

## Finding team IDs

Enable `LogKitRequests = true` in `BepInEx/config/fm26.community.customkits.cfg`, start a match, and check `BepInEx/LogOutput.log`:

```
[Kit] ResolveKitResourceID time=339 uniforme=home recurso=kit3d_339_home
```

`339` is the team ID. Name your kit file `339_home.png`.

## Name replacements (nomes.txt)

```
# SIGLA=Full name (one per line)
VDG=Vasco da Gama
```

## Compiling from source

```bash
~/.dotnet/dotnet build -c Release
```

Output: `bin/Release/net6.0/FM26CustomKits.dll`

## License

MIT
