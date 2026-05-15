using HeathenEngineering.PhysKit;
using UnityEngine;

namespace TeleportTrap;

public class Init
{
    public static void Run()
    {
        var trap = SpawnerManager.NameToWeaponDict["Teleport Mine"].GetComponent<WeaponHandSpawner>().objToSpawn;

        if (trap.TryGetComponent(out TrapLink link))
        {
            Object.Destroy(link);
        }

        SpawnerManager.NameToWeaponDict["Teleport Mine"].AddComponent<TrapLink>();
        trap.AddComponent<TrapLink>();
    }
}