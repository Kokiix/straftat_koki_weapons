using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
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
        newTrap.GetComponent<ProximityMine>().activated = false;
        newTrap.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);
        newTrap.GetComponent<ProximityMine>().sync___set_value_weapon(__instance, true);
        InstanceFinder.ServerManager.Spawn(newTrap);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            ((TrapLink)TeleportTrap.GetTrapLink(otherTrap)).otherTrap = newTrap;
            ((TrapLink)TeleportTrap.GetTrapLink(newTrap)).otherTrap = otherTrap;

            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            newTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
        }
        else
            connector.otherTrap = newTrap;
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPrefix]
    static bool DetectExplosion(ProximityMine __instance)
    {
        // if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return true;

        // GameObject otherTrap = ((TrapLink)TeleportTrap.GetTrapLink(__instance.gameObject)).otherTrap;

        // if (!InstanceFinder.IsServer || __instance.sync___get_value_detonated() || !otherTrap) return false;

        // if (InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob))
        // {
        //     __instance.ChangeState();
        //     __instance.HandleExplosion();
        // }
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool HandleExplosion(ProximityMine __instance)
    {
        KokiDebug.Log($"explosion server: {InstanceFinder.IsServer}   client: {InstanceFinder.IsClient}");
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