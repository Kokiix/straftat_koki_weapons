using System.Collections;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;
using Steamworks;

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

        ProximityMine mine = newTrap.GetComponent<ProximityMine>();
        mine.activated = false;
        mine.canActivate = false;
        mine.sync___set_value__rootObject(__instance.rootObject, true);
        mine.sync___set_value_weapon(__instance, true);
        InstanceFinder.ServerManager.Spawn(newTrap);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            TeleportTrap.GetTrapLink(otherTrap).otherTrap = newTrap;
            TeleportTrap.GetTrapLink(newTrap).otherTrap = otherTrap;

            ProximityMine otherMine = otherTrap.GetComponent<ProximityMine>();
            otherMine.StartCoroutine(ActivateTPMine(otherMine));
            mine.StartCoroutine(ActivateTPMine(mine));
        }
        else
            connector.otherTrap = newTrap;

        MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
        nameof(CustomWeaponNetworkManager.DisplayClientVisual), ReliableType.Reliable,
        newTrap.GetComponent<NetworkObject>().ObjectId, nameof(TeleportTrap.ConvertToPhysTPTrap), false);
        return false;
    }

    static IEnumerator ActivateTPMine(ProximityMine __instance)
    {
        yield return new WaitForSeconds(1);
        __instance.transform.Find("radius(Clone)").gameObject.SetActive(true);
        __instance.canExplode = true;
    }


    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPrefix]
    static bool DetectExplosion(ProximityMine __instance)
    {
        if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return true;

        GameObject otherTrap = TeleportTrap.GetTrapLink(__instance.gameObject).otherTrap;

        if (__instance.sync___get_value_detonated() || !otherTrap || !__instance.canExplode) return false;

        __instance.ChangeState();
        __instance.HandleExplosion();

        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool HandleExplosion(ProximityMine __instance)
    {
        if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return true;

        ProximityMine otherMine = TeleportTrap.GetTrapLink(__instance.gameObject).otherTrap.GetComponent<ProximityMine>();
        __instance.sync___set_value_detonated(true, true);
        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        if (!otherMine.sync___get_value_detonated())
        {
            otherMine.ChangeState();
            otherMine.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    if (fpc.Owner.IsLocalClient)
                    {
                        KokiDebug.Log("teleporting host");
                        fpc.Teleport(otherMine.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                    }
                    else
                    {
                        KokiDebug.Log("teleporting not host");
                        ulong.TryParse(fpc.Owner.GetAddress(), out ulong steamID);
                        MyceliumNetwork.RPCTarget(CustomWeaponNetworkManager.MyceliumID, nameof(CustomWeaponNetworkManager.TeleportClient), (CSteamID)steamID, ReliableType.Reliable, otherMine.transform.position);
                    }
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "Start")]
    [HarmonyPrefix]
    static bool HandleMineActivation(ProximityMine __instance)
    {
        return !TeleportTrap.GetTrapLink(__instance.gameObject);
    }

    [HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
    [HarmonyPrefix]
    static bool ExplodeTPTrapOnHit(Weapon __instance, GameObject obj)
    {
        GameObject trap = obj.transform.root.gameObject;
        if (obj.CompareTag("Mine") && TeleportTrap.GetTrapLink(trap))
        {
            GameObject otherTrap = TeleportTrap.GetTrapLink(trap).otherTrap;
            if (otherTrap)
                otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(false);
            obj.transform.root.GetComponent<ProximityMine>().ChangeState();
            obj.transform.root.GetComponent<ProximityMine>().ExplodeServer();
            return false;
        }

        return true;
    }
}