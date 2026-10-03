# Roberts Space Industries / Star Citizen Library Plugin for Playnite

A native Playnite library plugin that automatically detects, imports, and launches Star Citizen channels (**LIVE**, **PTU**, **EPTU**, **TECH-PREVIEW**) without any manual executable or path configuration.

---

## Features

- **Zero-Configuration Detection**:
  - Automatically scans all fixed and removable drives for Star Citizen installations (`\StarCitizen\LIVE`, `PTU`, etc.).
  - Extracts the exact game version and branch from `build_manifest.id` (or fallback to binary file version info).
- **RSI Launcher Integration**:
  - Automatically detects `RSI Launcher.exe` and provides quick access via Playnite's client menu.
- **Flexible Play Actions**:
  - Launch Star Citizen directly (via EAC launcher `StarCitizen_Launcher.exe` / `Bin64\StarCitizen.exe`).
  - Option to launch via the official RSI Launcher instead.
- **Channel Filters & Settings**:
  - Configurable in Playnite settings: custom directory override and channel import toggles.
- **Rich Metadata & Artworks**:
  - Ships with high-resolution official key art, logos, genres, platforms, and official community links (RSI Status, Comm-Links, Issue Council, Erkul).

---

## Installation

1. Download the latest `.pext` package from the [Releases](https://github.com/gOOvER/Playnite-StarCitizen-Library/releases) page.
2. Double-click the `.pext` file to install it into Playnite, or drag and drop it into Playnite.
3. Restart Playnite.
4. Press **F5** (Update Game Library) — Star Citizen will appear in your library under the **Roberts Space Industries** library filter!

---

## Building from Source

Requirements:
- .NET SDK (supports `net462`)
- Playnite 9 / 10 / 11

```powershell
# Run build & package script
.\build.ps1 -Configuration Release
```

The packaged extension will be generated in the project root as `*.pext`.

---

## Support & Donate

If you enjoy this plugin and want to support its ongoing development, feel free to buy me a coffee:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/goover)

---

## License

GPL-3.0 or later / MIT

