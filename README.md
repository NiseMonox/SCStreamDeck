![Star Citizen Stream Deck Plugin](./docs/assets/images/scsd-banner.png)

> **About this fork:** a fork of [ROBdk97/SCStreamDeck](https://github.com/ROBdk97/SCStreamDeck) v1.2.4.1 (itself a fork of
> [Jarex985/SCStreamDeck](https://github.com/Jarex985/SCStreamDeck)), checked against Star Citizen 4.10 LIVE, with these fixes:
>
> - Bindings cleared in-game (`<rebind input="kb1_ "/>` in `actionmaps.xml`) are honored instead of falling back to the default key.
> - A rebind's own `activationMode` (e.g. a key bound by double-tapping it) is honored.
> - Double-tap activation modes really tap twice, so actions such as *Power Throttle Max* (double-tap F10) fire in-game.
> - `global.ini` keys with a `,P` marker resolve, and categories missing from `global.ini` fall back to the action map name, so
>   labels no longer show up as raw `@ui_...` keys.
> - `Toggle Key` honors double-tap and long-press bindings, click sounds follow the default playback device, and settings
>   survive plugin updates. See the [changelog](CHANGELOG.md) for everything else.
> - The plugin UUID is `com.nisemonox.scstreamdeck`, so it installs next to the upstream plugin instead of replacing it.

User Guide: [docs](docs/index.md)  
Report bugs / feature requests: https://github.com/NiseMonox/SCStreamDeck/issues

<br>

> [!WARNING]
> **This site is not endorsed by or affiliated with the Cloud Imperium or Roberts Space Industries group of companies. 
> All game content and materials are copyright Cloud Imperium Rights LLC and Cloud Imperium Rights Ltd.. Star Citizen®, 
> Squadron 42®, Roberts Space Industries®, and Cloud Imperium® are registered trademarks of Cloud Imperium Rights LLC. All rights reserved.**  

<br>

## Requirements

[![Windows 10+](https://img.shields.io/badge/Windows-10%2B-blue?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![Stream Deck v6.4+](https://img.shields.io/badge/Stream%20Deck%20App-6.4%2B-purple?logo=elgato&logoColor=white)](https://www.elgato.com/s/stream-deck-app)

## Installation

There is no prebuilt release of this fork yet. Build and install from source with the
[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (the plugin runs on the .NET 10 Desktop Runtime x64):

```powershell
.\Tools\install-local.ps1
```

The script builds the plugin in Release, copies `PluginCore\bin\Release\com.nisemonox.scstreamdeck.sdPlugin` into
`%APPDATA%\Elgato\StreamDeck\Plugins` and restarts Stream Deck. Plugin settings and the generated keybindings live in
`%LOCALAPPDATA%\com.nisemonox.scstreamdeck`, so they survive reinstalls and updates.

Pushing a `v*` tag runs the release workflow, which publishes ready-to-install `.streamDeckPlugin` packages:

| Method | Package | Runtime requirement |
| --- | --- | --- |
| Simple | `com.nisemonox.scstreamdeck.runtime-included.streamDeckPlugin` | None |
| Advanced | `com.nisemonox.scstreamdeck.runtime-required.streamDeckPlugin` | Install [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0) |

## Project Info

### Status
[![GitHub release](https://img.shields.io/github/release/NiseMonox/SCStreamDeck?include_prereleases=&sort=semver&color=2ea44f)](https://github.com/NiseMonox/SCStreamDeck/releases/)
[![License](https://img.shields.io/badge/License-MIT-2ea44f)](LICENSE.md)
[![Contributions - welcome](https://img.shields.io/badge/Contributions-welcome-2ea44f)](CONTRIBUTING.md)  

[![CI](https://github.com/NiseMonox/SCStreamDeck/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/NiseMonox/SCStreamDeck/actions/workflows/ci.yml)

### Programming Languages
[![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp)
[![HTML5](https://img.shields.io/badge/HTML5-E34F26?logo=html5&logoColor=white)](https://developer.mozilla.org/docs/Web/HTML)
[![CSS3](https://img.shields.io/badge/CSS3-1572B6?logo=css3&logoColor=white)](https://developer.mozilla.org/docs/Web/CSS)
[![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?logo=javascript&logoColor=black)](https://developer.mozilla.org/docs/Web/JavaScript)



## Current Features

- **Adaptive Key**: A key that executes keybindings based on the activation mode for a given binding.
    - *Example:* Two in-game bindings (Tap vs Hold) on `Num-` executes only the Tap function when this is the assigned function.
- **Adaptive Dial** *(Stream Deck+)*: A dial that maps separate Star Citizen functions to rotate left, rotate right, and dial push. Rotation fires the assigned function once per tick; push respects the action's activation mode.
- **Toggle Key**: A key that toggles between two states (e.g., landing gear up/down). Can be reset to match the current in-game state on de-sync.
- **Control Panel Key**: A dedicated key for managing global plugin settings such as themes, preferred channel selection, per-channel installation overrides, and plugin language.
- **Auto-Detection of Star Citizen Installation Path**: Automatically detects the installation path of Star Citizen.
- **Multiple Channels Support**: Supports LIVE, HOTFIX, PTU, EPTU, and TECH-PREVIEW installations.
- **Plugin UI Localization**: Built-in plugin and Property Inspector localization for English, German, French, and Spanish, with auto-detect and a Control Panel override.
- **Mouse Wheel Support**: Supports mouse wheel actions for bindings that use mouse wheel input (Mouse Wheel Up/Down).
- **Custom Language Support**: Supports custom language files for localization when using custom global.ini from the community, e.g. [StarCitizen-Deutsch-INI by rjcncpt](https://github.com/rjcncpt/StarCitizen-Deutsch-INI).
- **Theme Support**: Themes for customizing the appearance of the plugin. Includes a template for creating your own themes!
- **Click Sound**: Provides audio feedback on key presses with configurable sound files (.wav and .mp3).


## Installation

See the full installation guide: https://nisemonox.github.io/SCStreamDeck/install/

## Known Limitations

**Mouse buttons (mouse1 - mouse5) don't work while moving the mouse in Star Citizen**  

Star Citizen can ignore synthetic mouse button events sent by Windows user-mode injection (the plugin uses `SendInput`) while the game is actively reading mouse movement for aiming/flying (relative mouse input).   
This can make Stream Deck-triggered mouse clicks (mouse1–mouse5, including MMB/mouse3) unreliable unless the mouse is perfectly still.

Workarounds:

- Prefer binding Stream Deck actions to **keyboard keys** in Star Citizen.

Why not “fix” it in the plugin?

- The "reliable" approach is HID-level injection (virtual mouse / driver). This plugin intentionally avoids driver-based input injection due to potential anti-cheat / ToS risk.

## Credits

Star Citizen Stream Deck Plugin uses the following open-source projects and libraries:

- [streamdeck-tools by BarRaider](https://github.com/BarRaider/streamdeck-tools) - for the excellent C# library.
- [sdpi-components by GeekyEggo](https://github.com/GeekyEggo/sdpi-components) - for the excellent Stream Deck Property Inspector components.
- [InputSimulatorPlus by TChatzigiannakis](https://github.com/TChatzigiannakis/InputSimulatorPlus) - (although i think this might be a modified fork of BarRaider, will verify later)
- [NAudio by Mark Heath](https://github.com/naudio/NAudio) - for audio playback support.

## Acknowledgements

This project was inspired by the following repositories (code rewritten from scratch and optimized):

- [unp4k by dolkensp](https://github.com/dolkensp/unp4k) - for letting me browse through the P4K file and understand its structure.
- [SCJMapper-V2 by SCToolsfactory](https://github.com/SCToolsfactory/SCJMapper-V2) - for the great work on Star Citizen keybindings extraction.
- [streamdeck-starcitizen by mhwlng](https://github.com/mhwlng/streamdeck-starcitizen) - for the initial idea of a Stream Deck plugin for Star Citizen. :)
- [SCStreamDeck by Jarex985](https://github.com/Jarex985/SCStreamDeck) - Forked/Optimized/Modernized because the owner was inactive
- [SCStreamDeck by ROBdk97](https://github.com/ROBdk97/SCStreamDeck) - the maintained fork (Adaptive Dial, .NET 10, plugin localization) this repository is based on
