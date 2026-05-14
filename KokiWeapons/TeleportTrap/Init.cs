using HeathenEngineering.PhysKit;
using UnityEngine;

namespace TeleportTrap;

public class Init
{
    public static void Run()
    {
        var trap = SpawnerManager.NameToWeaponDict["Teleport Mine"].GetComponent<WeaponHandSpawner>().objToSpawn;

        if (KokiWeaponsPlugin.DebugGetComponent(trap, typeof(TrapLink)))
        {
            Object.Destroy(KokiWeaponsPlugin.DebugGetComponent(trap, typeof(TrapLink)));
        }

        trap.AddComponent<TrapLink>();
    }
}

public class TrapLink : MonoBehaviour
{
    public GameObject otherTrap;
}