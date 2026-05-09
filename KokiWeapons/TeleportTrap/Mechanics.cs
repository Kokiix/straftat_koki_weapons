using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

public static class TPTrapMechanics
{
    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static bool PlaceMine(WeaponHandSpawner __instance, GameObject obj, Vector3 position, Quaternion rotation)
    {
        TrapLink connector = __instance.gameObject.GetComponent<TrapLink>();
        if (PauseManager.BetweenRounds || !connector) return true;

        GameObject newTrap = UnityEngine.Object.Instantiate(TeleportTrap.TemplatePhysGameObject, position, rotation);
        newTrap.SetActive(true);

        ProximityMine mine = newTrap.GetComponent<ProximityMine>();
        mine.activated = false;

        InstanceFinder.ServerManager.Spawn(newTrap);

        newTrap.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            NetworkObject otherNob = otherTrap.GetComponent<NetworkObject>();
            NetworkObject thisNob = newTrap.GetComponent<NetworkObject>();
            __instance.damage = otherNob.ObjectId;

            Weapon otherTrapWeapon = otherTrap.GetComponent<ProximityMine>().sync___get_value_weapon();
            otherTrapWeapon.bulletsAmount = thisNob.ObjectId;
            otherTrap.GetComponent<ProximityMine>().sync___set_value_weapon(otherTrapWeapon, true);

            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            newTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
        }
        else
        {
            __instance.damage = -1;
            __instance.bulletsAmount = -1;
            connector.otherTrap = newTrap;
        }

        newTrap.GetComponent<ProximityMine>().sync___set_value_weapon(__instance, true);
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPrefix]
    static bool DetectExplosion(ProximityMine __instance)
    {
        if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return true;
        if (!InstanceFinder.IsServer || __instance.sync___get_value_detonated()) return false;

        Weapon sharedWeapon = __instance.sync___get_value_weapon();
        int otherTrapID;
        if (sharedWeapon.bulletsAmount == -1 || sharedWeapon.damage == -1f) return false;
        if (sharedWeapon.damage == __instance.gameObject.GetComponent<NetworkObject>().ObjectId)
            otherTrapID = sharedWeapon.bulletsAmount;
        else
            otherTrapID = (int)sharedWeapon.damage;

        if (InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob))
        {
            __instance.ChangeState();
            __instance.HandleExplosion();
        }
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool HandleExplosion(ProximityMine __instance)
    {

        if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return true;

        Weapon sharedWeapon = __instance.sync___get_value_weapon();
        int otherTrapID;
        if (sharedWeapon.damage == __instance.gameObject.GetComponent<NetworkObject>().ObjectId)
            otherTrapID = sharedWeapon.bulletsAmount;
        else
            otherTrapID = (int)sharedWeapon.damage;

        InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob);
        ProximityMine otherTrap = otherNob.gameObject.GetComponent<ProximityMine>();
        __instance.sync___set_value_detonated(true, true);
        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        if (!otherTrap.sync___get_value_detonated())
        {
            otherTrap.ChangeState();
            otherTrap.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    fpc.Teleport(otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }
}