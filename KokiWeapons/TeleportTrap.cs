using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    private static GameObject _gameObject;
    public static GameObject BaseGrenadeMesh;
    public static GameObject PhysGrenadeMesh;
    public static GameObject GameObject()
    {
        if (_gameObject) return _gameObject;

        _gameObject = SpawnerManager.NameToWeaponDict["APMine"];

        WeaponHandSpawner spawner = _gameObject.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        // Communicate to patch by flagging both at once
        spawner.proximityMine = true;
        spawner.apmine = true;

        // Swap visuals
        // Transform baseVisualParent = _gameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        // baseVisualParent.Find("SM_Grenadino_01").gameObject.SetActive(false);
        // BaseGrenadeMesh.transform.SetParent(baseVisualParent);

        // Transform physicsObj = _gameObject.GetComponent<DualLauncher>().trickShot.template.gameObject.transform;
        // physicsObj.Find("Graph").gameObject.SetActive(false);
        // PhysGrenadeMesh.name = "Graph";
        // PhysGrenadeMesh.transform.SetParent(physicsObj);

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