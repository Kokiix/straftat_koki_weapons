
<img width="256" height="256" alt="icon" src="https://github.com/user-attachments/assets/4ab5f197-8a70-4849-a627-ef2d9aab9f36" />

---

# How does it work
*Credit to Znoki, DankoBanko for modeling, testing, and ideas*

> [!NOTE]
> This thing is probably going to be constantly outdated lol
> 
> The target audience is myself, so the level of detail will reflect that.


## Setup Process

1. Load the game (from root directory!) into Asset Ripper. Set the script export settings to stubs/(method stripping) to avoid compile errors.

2. Use search and dependency feature to find out what assetbundles are necessary for the mod, then export and open.

    (for STRAFTAT, something like just shareassets0 and globalassets include everything but levels)

3. For hot reload compat, split codebase into 2 subprojects, one for the main project and one for components. Because of the way C# works, type references in getComponent<TYPE> won't be able to find the correct type if the component DLL is reloaded. Splitting the project allows for reloading at least some code when not working on components.

## General

**Registration**

- Weapons go into `SpawnerManager`, in `AllWeapons`, `NameToWeaponDict`, and `NameToIndexDict` when `SpawnerManager.PopulateAllWeapons` runs, which is just once at the start of the game. 
- Networked Gameobjects go into `NetworkManager.SpawnablePrefabs` during `NetworkManager.Start`; a new NM is created every time the player goes to title screen/joins lobby afaik

## TP Mine

- The mod consists of 2 patches, 2 prefabs made in Unity, 2 components, and a networking component attached to the plugin instance.

**Prefabs**
- The item/physics variants of the mine are basically just copied from AP mine, with the `ProximityMine` component removed.

**Components**
- There's a primary component that replaces `ProximityMine`, `TPTrap`, as well as an auxillary component `TPLink`.
- When a mine is placed, TPLink stores that mine's NetworkObject (nob) ID. When the second is placed, that ID is retrieved and broadcast together with the new ID to link the two mines. This is actually unnecessary rn.. as 

**Patches**
- There's a patch into the ServerRPC spawn item logic that 