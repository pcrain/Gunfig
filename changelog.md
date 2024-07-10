# Changelog

## 1.1.1 (2024-07-09)

- Added config option for opening Ammonomicon instantly
- Added config option for unlimited active item slots
- Added config option for infinite Hegemony Credits

## 1.1.0 (2024-07-07)

#### API Changes
- Added ability to create submenus via `AddSubMenu()`
- Added ability to dynamically add options to scroll boxes at runtime via `AddDynamicOptionToScrollBox()`

#### User Facing Changes
- Added config option for allowing co-op players to select their character directly from the Breach
- Added config option for allowing player to spawn in items and guns directly from Ammonomicon
- Added ability to back out of one level of menus at a time
- Fixed vanilla bug where keyboard / controller inputs can't navigate single-item menus correctly

## 1.0.3 (2024-01-19)
- Switched to an alternate method for loading configuration options early since the intro sequence hook ended up breaking things

## 1.0.2 (2024-01-19)
- Added hook to make sure default values for configuration options are loaded earlier in the intro sequence (before quickstarting or entering the Breach)

## 1.0.1 (2023-12-20)
- Simplified logic for "Auto-fire Semi-Automatic Weapons" hook to fix compatibility issues with GungeonCraft and other mods

## 1.0.0 (2023-12-17)
- Initial Release! :D
