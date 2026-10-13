# Changelog

**Version format:** `MAJOR.MINOR.PATCH.BUILD`

| Position  | Meaning                                            | Example / Usage                                                                                                                          |
| --------- | -------------------------------------------------- |------------------------------------------------------------------------------------------------------------------------------------------|
| **MAJOR** | Major version / breaking change in behavior        | `1.0.0.0` = First stable, feature-complete release for the planned scope.                                                                |
| **MINOR** | Big user-facing release / new feature line         | `0.1.0.0` = New features, UX overhauls, or major internal changes that affect users. Breaking changes will be called out with a warning. |
| **PATCH** | Smaller compatible improvements + bug fixes        | `0.0.1.0` = Small UX improvements and bug fixes. Existing settings shouldn't be affected.                                                |
| **BUILD** | Internal-only / no user-visible changes            | `0.0.0.1` = Code cleanup, refactors, tests without functional changes.                                                                   |

Notes:
- Versions below `1.0.0.0` are pre-release.
- If a release resets settings or requires manual steps, it will be explicitly highlighted.

---

## v1.3.1.0 - Patch Release

### Bug Fixes

- Actions that are declared only with `onPress="1"` (e.g. *Accept Invite* on `[`, *Throttle - Cruise Mode - Toggle* on `LAlt+C`, director-mode FoV / depth of field) were inferred as `double_tap` and, since v1.3.0.0, pressed twice. They are plain presses again; only actions that really say `double_tap` (or that you bound by double-tapping) are tapped twice.
- The `activationMode` attribute is read regardless of its casing (`ActivationMode` / `activationmode` are used by 22 actions, e.g. *Stopwatch Reset (Long Press)*).
- `Toggle Key` now performs the activation its function needs (double tap, or holding a long-press binding past its threshold) instead of always sending one short press.
- Key down and key up of the same function are executed in the order they arrive, and a release can no longer interleave with a delayed hold that is just starting; either could leave a key held down in the game.
- A delayed hold on a mouse-wheel binding stops scrolling when the key is released.
- Click sounds follow the current default playback device. The audio output is only opened while a sound plays, instead of holding a silent stream open all the time, and a missing playback device no longer breaks every key.
- Cached keybindings are regenerated after a plugin update, so parser fixes take effect immediately.
- Folders that merely contain `Windows` or `System32` in their name (e.g. a user called `WindowsUser`) are no longer rejected as unsafe; only the real Windows directory is.
- `defaultProfile.xml` is recognised as binary CryXml by its signature. Before, a stray `<` byte in its header would have made the whole keybinding import fail.
- Function names missing from a translated `global.ini` fall back to English, like in the game, and the 17 label keys without a `ui_` prefix (e.g. *Toggle Equip Helmet*) are resolved.
- A function whose keyboard slot holds a mouse axis (e.g. `LAlt+maxis_z`) is no longer sent as its bare modifier key.

### Features / Improvements

- Plugin settings and the generated keybindings moved from the plugin folder to `%LOCALAPPDATA%\com.nisemonox.scstreamdeck`, so they survive plugin updates. Existing settings are moved over on first start.
- The parsed `global.ini` (about 90,000 entries) is released once the function names have been applied.

### Internal / Refactor

- One shared `KeybindingAction.GetExecutableBinding()` replaces three copies; `ExecutePress` delegates to `ExecutePressNoRepeat`.

## v1.3.0.0 - Minor Release (NiseMonox fork)

> [!WARNING]
> The plugin UUID is now `com.nisemonox.scstreamdeck`. It installs next to `com.robdk97.scstreamdeck` instead of updating
> it, so keys set up with the upstream plugin have to be added again.

### Bug Fixes

- Bindings cleared in-game (`<rebind input="kb1_ "/>` in `actionmaps.xml`) are no longer ignored, so the plugin stops showing and sending the default key.
- A rebind's own `activationMode` (e.g. a key bound in-game by double-tapping it) now overrides the action's default activation mode.
- `double_tap` / `double_tap_nonblocking` actions now send two taps instead of one, so they fire in-game.
- `global.ini` keys with a `,P` marker (e.g. `ui_CIMiningMode,P=`) are resolved, and categories whose key is missing from `global.ini` fall back to the action map label. On 4.10 LIVE this leaves 2 of 753 labels unresolved (was 63).

### Internal / Refactor

- `global.json` rolls forward to newer .NET 10 SDK feature bands.
- The docs workflow no longer deploys to GitHub Pages on every push to `main`; run it manually.
- Added `Tools/install-local.ps1` to build, install into Stream Deck and restart it.

## v1.2.4.1 - Patch Release

### Bug Fixes

- Fixed issue #7 where some mouse wheel/mouse-axis actions (for example `Mining Increase Laser Power`) were missing from the `Adaptive Dial` function dropdown.
- Fixed function details in the Property Inspector so mouse-axis bindings are shown under `Mouse` instead of incorrectly appearing as `Unbound`.

### Internal / Refactor

- Updated selected `PluginCore` NuGet dependencies to newer patch versions (`Microsoft.CodeAnalysis.NetAnalyzers`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Win32.SystemEvents`, and `System.Drawing.Common`).

## v1.2.3.0 - Minor Release

### Features / Improvements

- Added a `Touch Strip Text` toggle for `Adaptive Dial` so the dial title can be shown or hidden on the Stream Deck+ touch strip.

### Bug Fixes

- Reworked the Adaptive Dial feedback layouts to remove the default grey bottom-right bar.
- Increased the usable icon area and balanced the captioned layout so the icon no longer looks cramped next to the touch-strip text.

## v1.2.2.1 - Build Release

### Internal / Refactor

- Updated multiple NuGet dependencies to their latest versions, including analyzers, DI, NLog, SystemEvents, ZstdSharp, FluentAssertions, and test tools. These updates bring bug fixes, improvements, and new features.

## v1.2.2.0 - Patch Release

### Bug Fixes

- Fixed a Property Inspector localization security issue by removing HTML-based translation rendering from the shared i18n helper and switching localized help text to plain text.

## v1.2.1.0 - Patch Release

### Features / Improvements

- Improved startup and refresh performance by reusing generated keybinding data in memory instead of immediately reloading it from disk after regeneration.
- Improved keybinding/profile detection performance by caching resolved `actionmaps.xml` paths and watching only the resolved profile directory instead of the full channel tree.
- Improved `Data.p4k` processing performance by reusing the active archive session and caching directory scan results during regeneration/localization work.

### Bug Fixes

- Fixed unnecessary repeated plugin state file reads/writes during normal operation by caching the active `PluginState` in memory.

### Internal / Refactor

- Updated the keybinding output pipeline to return the generated `KeybindingDataFile`, allowing downstream services to reuse the in-memory data directly.
- Expanded affected unit tests to cover the optimized initialization and keybinding processing flow.

### Documentation

- Updated the docs and README to use direct latest-release download links for both plugin packages:
  - `com.robdk97.scstreamdeck.runtime-included.streamDeckPlugin`
  - `com.robdk97.scstreamdeck.runtime-required.streamDeckPlugin`
- Updated the usage/install documentation to reflect the current `Control Panel` options, including `TECH-PREVIEW` channel support and the `Plugin Language` auto-detect/manual override setting.

## v1.2.0.0 - Minor Release

### Features / Improvements

- Added `Adaptive Dial` action for Stream Deck+ devices. Supports assigning separate Star Citizen functions to rotate left, rotate right, and dial push. Rotation executes the assigned function once per tick. Push respects the action's activation mode (e.g., Tap vs Hold).
- Click sound support for the `Adaptive Dial` (same `.wav`/`.mp3` configuration as `Adaptive Key`).
- Added dual release packages for installation:
  - `runtime-included` for the simple install path with the .NET runtime bundled
  - `runtime-required` for the smaller advanced install path that uses the system-installed .NET 10 Desktop Runtime x64
- Updated the plugin/runtime target to `.NET 10`.
- Added plugin UI localization for English, German, French, and Spanish.
- Added a `Plugin Language` setting in the `Control Panel` with auto-detect and manual override support.
- Added `TECH-PREVIEW` as a supported Star Citizen channel alongside `LIVE`, `HOTFIX`, `PTU`, and `EPTU`.

### Internal / Refactor

- `SCActionBase` and `ControlPanelKey` now inherit from `KeypadBase` instead of `KeyAndEncoderBase`, removing stub dial/touchpad overrides that were never used for key actions.
- Added explicit `Controllers` declarations (`Keypad` / `Encoder`) to all actions in `manifest.json`.
- Added unit tests for `AdaptiveDial` covering rotation resolution and executable binding resolution.
- Refactored Property Inspector function picker logic into shared helpers (`SCPI.functionPicker`) used by both key and dial scripts, reducing duplicated code.

### Documentation

- Updated the docs for the two install methods (`Simple` and `Advanced`) and the new release package names.
- Documented `Control Panel` language selection, plugin localization support, and `TECH-PREVIEW` channel support.

## v1.1.3.0 - Patch Release

### Features / Improvements

- Bindings that use mwheel_up/mwheel_down will be retriggered while key is held down.

### Bug Fixes

- Fixed an Issue where some modifier keys didn't properly execute when combined with a mouse button.
- Fixed an Issue where `Hold` ActivationModes triggered multiple times when a binding had a modifier key

## v1.1.2.0 - Minor Release

### Features / Improvements

- Added `ActionMapsWatcherService` to monitor changes in `actionmaps.xml` and auto-sync keybindings in Stream Deck profiles when changes are detected (e.g., after in-game rebinding).
- Toggle Key now only shows Actions that are considered to be a `Toggle` based on their metadata, improving usability and reducing confusion.

### Bug Fixes

- Fixed an issue where actions with the same name/binding but different category/map would not be parsed correctly, which could lead to one action incorrectly shown as `Unbound`.
- Fixed an issue where Toggle Key would not maintain its state when switching between different pages on the Stream Deck, leading to inconsistent behavior. Now only resets the state when the user explicitly triggers the reset, or the application restarts.
- Fixed an issue where some action bindings would not get parsed and show up as `Unbound` (e.g., `focus_on_chat_textinput` with `return` key).

### Documentation

- Added a note about mouse button bindings not working when moving the mouse. This is a Star Citizen (EAC) limitation which i can't do anything about.

### Internal / Refactor

- Introduced a keybindings JSON schema version to force-refresh cached keybindings when the data format changes.

## v1.0.2.0 - Patch Release
> [!NOTE]
> This update introduces a stable v2 identifier format for saved function bindings (v2|<actionName>|<mapName>).
> Existing Stream Deck profiles using the legacy format remain supported and will auto-migrate when the key becomes active/visible.

### Bug Fixes

- Fixed an issue where some keybindings would not get parsed correctly if it just contained a modifier (e.g. `Left Ctrl`), 
  or didn't have a UICategory assigned. 

## v1.0.1.0 - Patch Release

### Bug Fixes

- Fixed an issue where Data.p4k was not closed properly after reading, which could lead to file locks (e.g. when verifying game files in the launcher).

## v1.0.0.0 - Major Release
> [!NOTE]
> No breaking changes from v0.3.0.0. This is the first stable, feature-complete release for the planned scope.

### Features / Improvements

- Added `Toggle Key` to switch between two states, with an optional reset if the state gets out of sync
- Removed unbound functions category from the function dropdown
- Added a warning indicator for functions that have no keybinding
- Improved `Force Redetection` to re-scan `actionmaps.xml` from auto-detected installs and custom install paths, then refresh cached data

### Bug Fixes

- Various minor bug fixes and improvements

### Documentation

- Updated documentation to reflect new features and changes
- Added a legal note and “Made by the Community” badge to the docs site
- Minor spelling/grammar fixes across the docs

### Internal / Refactor

- Reduced technical debt (code cleanup + improved comments)

## v0.3.0.0 - Minor Release

### Features / Improvements

- Added a new `Control Panel` key along with a redesigned Property Inspector UI/UX
- Added theme switching support and a shared base CSS stylesheet so users can create custom themes
- Added the `Default` and `Crusader Blue` themes
- Added channel switching support to the `Control Panel` key
- Improved `Control Panel` UI/UX for channel overrides and state management (clearer loading/success/error states)
- Improved keybinding processing and action mapping (better label disambiguation and metadata usage)
- Improved installation detection UX/messaging
- Removed the `custom-paths.ini` workaround (custom paths are now managed via the `Control Panel` key)

### Bug Fixes

- None explicitly called out for this release (mostly new functionality plus stability/UX improvements)

### Internal / Refactor

- Major refactor/cleanup across keybinding services (executor/parser wiring simplifications; removed unused deps; some helpers moved to static)
- Reduced risk hotspots and improved maintainability by extracting logic into focused helpers/services
- Expanded and reorganized unit tests significantly (keybinding parsing/execution, activation modes, installation/state, localization, data parsing)
- Reduced reliance on `[ExcludeFromCodeCoverage]` by covering previously excluded logic with tests
- SonarQube/Rider-driven cleanup: reduced cognitive complexity, standardized formatting, removed dead code, tightened access modifiers
- DI/service initialization cleanup and consolidation

## v0.2.2.1

### Features / Improvements

- (Experimental) Added a click sound feature to provide audio feedback
- Sound file can be configured for every key in the Property Inspector
- Supported formats: `.wav` and `.mp3`
- Only supports the key press event for now; activation modes are ignored

### Bug Fixes

- None

### Internal / Refactor

- Major internal refactor of the test project structure and organization
- Removed `UnitTests` and added a `Tests` project with better structure
- Major refactor of the PluginCore project structure and organization
  (should not affect functionality; improves maintainability and readability)


## v0.2.1.0

### Features / Improvements

- Added `custom-paths.ini` as a backup plan if auto-detection still fails to locate the Star Citizen installation path
- Users can manually add installation paths to `custom-paths.ini` located in the plugin root folder
- The plugin checks these paths if auto-detection does not find a valid installation
- Instructions for editing `custom-paths.ini` are included in the file

Path to plugin: `%APPDATA%\Elgato\StreamDeck\Plugins\com.robdk97.scstreamdeck.sdPlugin`

### Bug Fixes

- Fixed an issue where auto-detection was too restrictive and would miss valid installation paths

### Internal / Refactor

- Improved error handling and logging for installation path detection


## v0.2.0.0 - Minor Release

> [!WARNING]
> **Note: Keybindings from earlier Plugin Versions inside Stream Deck App will be reset due to internal changes.**

### Features / Improvements

- None

### Bug Fixes

- Corrected mouse wheel direction mapping for Star Citizen
- Fixed issues with action parsing and unbound actions
- Minor UI and CSS improvements

### Internal / Refactor

- Removed some debug logging and cleaned up comments
- Updated collection initializations
- Refactored handler registration and restricted visibility for cleaner architecture
- Streamlined `SmartToggleHandler` execution for clarity and reliability
- Modularized input executor and keybinding loader for better error handling
- Consolidated `ActivationModeHandler` metadata into the execution context

## v0.1.0.2

### Features / Improvements

- None

### Bug Fixes

- Press & Delayed Press now execute correctly as long as the key is held down
- Updated CSS to prevent pointer events on dropdown overlays

### Internal / Refactor

- Cleaned up comments and removed unused code
- Improved logging messages for better clarity

## v0.1.0.1

### Features / Improvements

- Added a basic UI theme (Concierge color scheme)
- Added the default key SVG

### Bug Fixes

- Minor bug fixes

## v0.1.0.0

### Features / Improvements

- Initial release
