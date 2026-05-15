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
        TPTrap newComponent = trap.AddComponent<TPTrap>();
        newComponent.explosionVfx = KokiWeaponsPlugin.Bundle.LoadAsset<GameObject>("boom");
        newComponent.explosionAudio = KokiWeaponsPlugin.Bundle.LoadAsset<AudioClip>("boom");
    }
}