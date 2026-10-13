# Update / Uninstall

## Update

1. Uninstall the old version (see below).
2. Download the correct latest-release package:
   - [Simple / runtime included](https://github.com/NiseMonox/SCStreamDeck/releases/latest/download/com.nisemonox.scstreamdeck.runtime-included.streamDeckPlugin)
   - [Advanced / runtime required](https://github.com/NiseMonox/SCStreamDeck/releases/latest/download/com.nisemonox.scstreamdeck.runtime-required.streamDeckPlugin)
3. Double-click it to install.

!!! important "Uninstall required"
    You **need** to uninstall before installing a newer version. Keybindings are stored in your Stream Deck profile and will remain after uninstalling.

!!! note
    Your Stream Deck profile keeps the action placements. Since v1.3.1.0 the plugin settings (preferred channel, language override, theme, custom installation overrides) are stored in `%LOCALAPPDATA%\com.nisemonox.scstreamdeck` and survive a reinstall; when updating from an older version, open the `Control Panel` action and quickly verify them.

## Uninstall

1. Open Stream Deck.
2. Open `Settings`.
3. Go to the `Plugins` tab.
4. Select `Star Citizen Stream Deck`.
5. Right-click and choose `Uninstall`.
6. If this doesn't work, close the Stream Deck software, go to `%APPDATA%\Elgato\StreamDeck\Plugins`, and delete the folder named `com.nisemonox.scstreamdeck.sdPlugin`.


