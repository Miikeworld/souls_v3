# Souls-like Prototype

A 3D action-RPG prototype built in Unity. Create a character, enter the `Fantasy` arena, and fight a boss using Souls-style mechanics: bonfires, lock-on, rolls, and quick items.

## Scenes & Flow

The build includes three active scenes:

- **MainMenu** – start screen; choose to create a character.
- **CharacterCreation** – customize appearance, then save and load `Fantasy`.
- **Fantasy** – the main gameplay arena containing the boss and a bonfire.

`Hub` and `Dungeon` are disabled in Build Settings and not reachable from the game.

## Controls

| Action | Input |
|---|---|
| Move | W / A / S / D |
| Camera orbit | Move mouse |
| Zoom | Mouse wheel |
| Lock-on / release | Middle mouse button |
| Attack (melee combo) | Left mouse button |
| Roll / dodge | Space (tap) |
| Sprint | Space (hold) |
| Jump | F |
| Cast fireball | Q |
| Use quick item / flask | R |
| Interact (doors, pickups, bonfires) | E |
| Open / close bonfire menu | E near bonfire, then B or Escape |

## Core Systems

### Combat
- Left-click performs a 3-hit katana combo. Pressing attack again during an active swing queues the next combo step.
- Rolling and attacking cost stamina.
- Fireballs cost mana.
- Getting hit staggers the player briefly and adds camera shake.

### Lock-On
- Press middle mouse to lock onto the nearest enemy within range.
- The camera switches to a stable behind-the-shoulder view while locked.
- Press middle mouse again to release.

### Healing & Quick Items
- **R** uses the selected quick item. The first slot is always the healing flask (restores health and uses one potion charge).
- Scroll the mouse wheel to cycle through extra consumables in the inventory.
- Consumables restore health, mana, or stamina and are removed from the inventory on use.
- Potions are restored when resting at a bonfire.

### Bonfires
- Approach a bonfire and press **E** to light / rest at it.
- Resting fully heals the player, restores potions, and sets the respawn point.
- The bonfire menu has buttons for Rest, Travel (not implemented), and Level Up (not implemented).

### Death & Respawn
- When health reaches zero, the player dies and falls.
- Pressing the interact key after death respawns the player at the last rested bonfire.
- If no bonfire has been rested at, the player respawns at the scene's default starting point.

### Inventory & Items
- Items are stored in the `Inventory` singleton.
- Stackable items can stack up to their `maxStackSize`.
- Item pickups in the world use **E** to collect.
- Consumable items can be used from the quick item bar.

### Boss
- The `Fantasy` arena contains one boss (`Arcane Tyrant`).
- The boss attacks with projectiles, spells, AOE effects, and melee combos.
- A boss health bar appears at the bottom of the screen when the player is in range.

### Camera
- Free camera is a Cinemachine 3.x orbital camera driven by mouse look.
- During lock-on the camera switches to `CameraFollow` and tracks the locked target.
- Cursor is locked and hidden during gameplay; it is released when UI menus are open.

## Build Notes

- `Active Input Handling` is set to **Both** (Legacy Input Manager + Input System).
- The `InputSystem_Actions` asset is preloaded and used by the camera and UI.
- `CinemachineLockOnCamera` creates its virtual cameras at runtime.
- A fallback `MainCamera` with `CameraFollow` and `AudioListener` is created if no main camera exists in a scene.

## Key Scripts

- `GameManager` – singleton, scene initialization, player spawning, respawn tracking.
- `PlayerController` – movement, combat, rolls, jumps, healing, death.
- `Entity` – base health / stamina / mana / potion logic.
- `CinemachineLockOnCamera` – Cinemachine free-look and lock-on camera setup.
- `LockOnSystem` – target selection and re-acquisition.
- `BossController` – boss AI and attacks.
- `BossHealthBarUI` – boss health bar UI.
- `Bonfire` – rest / respawn point logic and UI.
- `Inventory` / `ItemData` / `ItemPickup` – item storage and collection.
- `QuickItemBar` – quick item selection and use.
- `SoulsUI` – souls counter.
- `PlayerInteract` – interaction prompt and `E` interaction.
- `LoadingScreen` – async scene loading screen.
- `CharacterCreationController` – character customization and save.

## Known Stubs / Not Yet Implemented

- Fast travel between bonfires (`Bonfire.OpenTravelMenu`).
- Character level-up / stat allocation (`Bonfire.OpenLevelUpMenu`).
- `Hub` and `Dungeon` scenes are not included in the active build.
