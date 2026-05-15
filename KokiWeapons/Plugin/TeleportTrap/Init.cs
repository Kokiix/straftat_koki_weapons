using HeathenEngineering.PhysKit;
using UnityEngine;

namespace TeleportTrap;

public class Init
{
    public static void Run()
    {
        var handItem = SpawnerManager.NameToWeaponDict["Teleport Mine"];
        var trap = handItem.GetComponent<WeaponHandSpawner>().objToSpawn;

        if (trap.TryGetComponent(out TPTrap trapComponent) && handItem.TryGetComponent(out TPLink link))
        {
            Object.Destroy(trapComponent);
            Object.Destroy(link);
        }

        handItem.AddComponent<TPLink>();
        trap.AddComponent<TPTrap>();
    }
}