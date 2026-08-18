# HDR Toggler

A lightweight Windows system tray utility for toggling HDR **per monitor**. Built on the Win32 `DisplayConfig` API.

## Why?

Windows only lets you toggle HDR globally with Win+Alt+B — it flips every display at once. If you have one HDR screen alongside a non-HDR one, that's useless. HDR Toggler gives you per-display control from a single click.

## Features

- **Per-monitor HDR toggle** — enable/disable HDR on each display independently
- **Global hotkeys** — assign a keyboard shortcut to any monitor (right-click tray → Hotkey Settings)
- **Start with Windows** — toggle in the tray menu to auto-launch at logon via Task Scheduler
- **HDR state restore** — remembers each monitor's HDR state and restores it on launch
- **Tray tooltip** — hover over the tray icon to see current HDR state for all monitors
- **Toggle notifications** — balloon popup confirms which monitor was toggled and to what state

## Download

Grab the latest `HDRToggler.exe` from [Releases](https://github.com/Baron-n/HDRTogglerTool/releases). It's a self-contained single file — no .NET runtime install needed.

A **one-click MSI installer** is also available under the same release.

## Usage

| Action | Result |
|---|---|
| Left-click tray icon | Opens the monitor panel |
| Click a monitor box | Toggles HDR on that display |
| Right-click tray icon | Opens quick-access menu |
| Hotkey Settings | Assign keyboard shortcuts per monitor |
| Start with Windows | Toggle auto-launch at logon |

## Building from source

```
dotnet build
```

Requires .NET 10 SDK and Windows 10 1803+.

## Notes

- Win+Alt+B is reserved by Windows for its own HDR toggle. Pick a different key combination when setting hotkeys.
- The app needs no elevated privileges for normal operation — the `DisplayConfig` API works from a standard user session.

## License

AGPL-3.0 — see [LICENSE.txt](LICENSE.txt)

---

**Credit:** Based on [HDRToggler](https://github.com/GiulioSamp/HDRToggler) by [GiulioSamp](https://github.com/GiulioSamp). Original project provided the core DisplayConfig integration and tray UI that this fork extends with hotkeys, startup persistence, and state management.
