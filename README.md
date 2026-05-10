cool screenshot here


---

> [!WARNING]
> When installing manually, ensure that the bundle file is located directly in the `plugins` folder or under `KokiWeapons` in the plugins folder.

# How does it work
*Credit to Znoki for modeling and DankoBanko for testing*

## Teleport Mine

- I refer to it internally as teleporttrap sometimes for some reason

### Init

- The server generally does spawning from an array `AllWeapons`, which holds `GameObjects` (GOs) that act as templates. Registering is as simple as inserting a modified GO into that and a couple other fields.
- (For mines, at least) Only a "base" item is directly registered globally, which in the case of a mine is the thing you pick up and can drop/throw around. Each base item carries with it a "template" instance of a physics item in its `WeaponHandSpawner.objToSpawn`

### Mechanics

- This weapon patches 4 functions: `WeaponHandSpawner.RpcLogic___SpawnObject_2587446063`, and `OnTriggerStay`, `HandleExplosion`, and `Start` under `ProximityMine`.

### Networking

- It was so jank when I started.. I was doing weird stuff with smuggling information through synced properties and trying to interact with fishnet.. I love mycelium and kestrel now
- Coming in I had no idea how networking worked. My current understanding for STRAFTAT is that the server  does everything — the client game basically turns into a video player that sends input.