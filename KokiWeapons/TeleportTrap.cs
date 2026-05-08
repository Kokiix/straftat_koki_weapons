using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject PhysGO;
    public static GameObject NonPhysGO;

    public static GameObject BaseMineMesh;
    public static GameObject PhysMineMesh;

    public static GameObject GetNonPhysGO()
    {
        if (NonPhysGO) return NonPhysGO;

        NonPhysGO = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        NonPhysGO.SetActive(false);

        NonPhysGO.AddComponent<TPTrapData>();

        ItemBehaviour ib = NonPhysGO.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport trap";

        WeaponHandSpawner spawner = NonPhysGO.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        // Swap visuals
        Transform baseVisualParent = NonPhysGO.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        GameObject meshInstance = UnityEngine.Object.Instantiate(BaseMineMesh);
        meshInstance.transform.SetParent(baseVisualParent);

        return NonPhysGO;
    }

    public static GameObject GetPhysGO(GameObject originalGO)
    {
        if (PhysGO) return PhysGO;

        PhysGO = UnityEngine.Object.Instantiate(originalGO);
        PhysGO.SetActive(false);

        // Swap visuals
        Transform baseVisualParent = PhysGO.transform;
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        GameObject meshInstance = UnityEngine.Object.Instantiate(PhysMineMesh);
        meshInstance.transform.SetParent(baseVisualParent);

        return PhysGO;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static void ExplodePrefix()
    {
        KokiWeaponsPlugin.Logger.LogError("general explosion");
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void PlaceObjectPrefix(WeaponHandSpawner __instance, ref GameObject obj)
    {
        if (__instance.gameObject.GetComponent<TPTrapData>())
        {
            KokiDebug.Log("placing teleport trap!");
            obj = GetPhysGO(originalGO: obj);
        }
    }
}

public class TPTrapData : MonoBehaviour
{
}