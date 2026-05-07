using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    private static GameObject _gameObject;
    public static GameObject BaseMineMesh;
    public static GameObject PhysGrenadeMesh;
    public static GameObject GameObject()
    {
        if (_gameObject) return _gameObject;

        _gameObject = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        _gameObject.SetActive(false);

        ItemBehaviour ib = _gameObject.GetComponent<ItemBehaviour>();
        ib.name = "teleport trap";

        WeaponHandSpawner spawner = _gameObject.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;
        // Communicate to patch by flagging both at once
        spawner.proximityMine = true;
        spawner.apmine = true;

        // Swap visuals
        Transform baseVisualParent = _gameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        BaseMineMesh.transform.SetParent(baseVisualParent);

        return _gameObject;
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