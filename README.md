cool screenshot here


---


**How to install (until i put it on thunderstore)**

1. Unzip into plugins (mod expects to find bundle at `plugins/KokiWeapons/kokiWeaponsBundle`)
2. Enable `HideManagerGameObject` in BepInEx config (`BepInEx/config/BepInEx.cfg`)

    ...the game kind of explodes if you don't.. if any devs know why pls help


# How does it work
*Credit to Znoki for modeling and DankoBanko for testing*

## Teleport Mine

- I refer to internally as teleporttrap for some reason

### Init

- The server generally does spawning from an array `AllWeapons`, which holds `GameObjects` (GOs) that act as templates. Registering is as simple as inserting a modified GO into that and a couple other fields.
- (I've only confirmed this for mines) Only a "base" item is directly registered globally, which in the case of a mine is the thing you pick up and can drop/throw around. Each base item carries with it a "template" instance of a physics item in its `WeaponHandSpawner.objToSpawn`

### Mechanics

- 

### Networking

- it is so jank.