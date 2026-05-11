
<img width="256" height="256" alt="icon(1)" src="https://github.com/user-attachments/assets/3584015d-18a0-4245-a35e-fe5f471d64fa" />

---

# How does it work
*Credit to Znoki for modeling and DankoBanko for testing*

## Teleport Mine

- I refer to it internally as teleporttrap sometimes for some reason

### Init

- The server generally does spawning from an array `AllWeapons`, which holds `GameObjects` (GOs) that act as templates. Registering is as simple as inserting a modified GO into that and a couple other fields.
- (For mines, at least) Only a "base" item is directly registered globally, which in the case of a mine is the thing you pick up and can drop/throw around. Each base item carries with it a "template" instance of a physics item in its `WeaponHandSpawner.objToSpawn`
- The process of creating the custom gameobject is generally:
1. Copy weapon gameobj from game
2. Edit `ItemBehavior` / `WeaponHandSpawner` properties
3. Replace mesh/colliders
4. Add custom components

### Mechanics

- This weapon patches 5 functions: 
    - `WeaponHandSpawner.RpcLogic___SpawnObject_2587446063`: set values and link the mines when they spawn in
    - `ProximityMine.OnTriggerStay`: determine if mine should explode (no if other one isnt spawned yet) 
    - `ProximityMine.HandleExplosion`: teleport
    - `ProximityMine.Start`: prevent the mine from doing typical mine things
    - `Weapon.TriggerEnvironment`: logic for when mine is shot

### Networking

- It was so jank when I started.. I was doing weird stuff with smuggling information through synced properties and trying to interact with fishnet.. I love mycelium and kestrel now
- Coming in I had no idea how networking worked. My current understanding for STRAFTAT is that the server  does everything — the client game basically turns into a video player that sends input. The information sent to this video player is highly limited for efficiency reasons — only certain properties are synced/networked. As a result, the client will always see server Teleport Mines as AP mines.
- The TP mine uses 2 RPCs that I'm just keeping generic to the mod: 
    - `DisplayClientVisual` to overwrite the AP mine with proper visuals on the client
    - `TeleportClient` because when the server does it the clients don't realize they've been teleported
