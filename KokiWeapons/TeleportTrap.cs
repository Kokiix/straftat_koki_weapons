using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject GO;
    public static GameObject BaseMineMesh;
    public static GameObject PhysGrenadeMesh;
    public static GameObject GameObject()
    {
        if (GO) return GO;

        GO = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        GO.SetActive(false);

        ItemBehaviour ib = GO.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport trap";

        WeaponHandSpawner spawner = GO.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;
        // Communicate to patch by flagging both at once
        spawner.proximityMine = true;
        spawner.apmine = true;

        // Swap visuals
        Transform baseVisualParent = GO.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        BaseMineMesh.transform.SetParent(baseVisualParent);

        return GO;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static void ExplodePrefix()
    {
        KokiWeaponsPlugin.Logger.LogError("sldfkj");
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void PlaceObjectPrefix(WeaponHandSpawner __instance, GameObject obj)
    {
        if (__instance.proximityMine && __instance.apmine)
        {
            KokiDebug.Log("teleport trap!");
        }
    }
}