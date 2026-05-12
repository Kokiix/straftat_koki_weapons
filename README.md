
<img width="256" height="256" alt="icon(1)" src="https://github.com/user-attachments/assets/3584015d-18a0-4245-a35e-fe5f471d64fa" />

---

# How does it work
*Credit to Znoki, DankoBanko for modeling, testing, and ideas*

> [!NOTE]
> This thing is probably going to be constantly outdated lol

## Teleport Mine

- I refer to it internally as teleporttrap sometimes for some reason

### GameObject

- The server generally does spawning from an array `AllWeapons`, which holds `GameObjects` (GOs) that act as templates. Registering is as simple as inserting a modified GO into that and a couple other fields.
- (For mines, at least) Only a "base" item is directly registered globally, which in the case of a mine is the thing you pick up and can drop/throw around. Each base item carries with it a "template" instance of a physics item in its `WeaponHandSpawner.objToSpawn`
- The process of creating the custom gameobject is generally:
1. Copy weapon gameobj from game
2. Edit `ItemBehavior` / `WeaponHandSpawner` properties
3. Replace mesh/colliders
4. Add custom components
- This conversion process is done in place so it can be performed on the client without messing things up.

### Mechanics

- This weapon patches 5 functions: 
    - `WeaponHandSpawner.RpcLogic___SpawnObject_2587446063`: set values and link the mines when they spawn in
    - `ProximityMine.OnTriggerStay`: determine if mine should explode (no, if other one isnt spawned yet) 
    - `ProximityMine.HandleExplosion`: teleport
    - `ProximityMine.Start`: prevent the mine from doing typical mine things
    - `Weapon.TriggerEnvironment`: logic for when mine is shot

### Networking

- It was so jank when I started.. I was doing weird stuff with smuggling information through synced properties and trying to interact with fishnet.. I love mycelium and kestrel now
- Coming in I had no idea how networking worked. My current understanding for STRAFTAT is that the server  does everything — the client game basically turns into a video player that sends input. The information sent to this video player is highly limited for efficiency reasons — only certain properties are synced/networked. As a result, the client will always see server Teleport Mines as AP mines, and the server needs to send RPCs for the client to convert those AP mines into TP mines.
- The following things are networked:

    **server to client**

    - mine is spawned (regular or phys) => overwrite ap mine visuals on client
    - on mine activation (when 2nd is placed) => display radius
    - when mine is shot => remove radius
    - when mine detonates => do teleport locally

    **client to server**

    - when mine is shot => destroy on server