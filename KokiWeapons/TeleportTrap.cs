using System;
using FishNet.Object;
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

        NonPhysGO.AddComponent<NonPhysTPTrapData>();

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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(NetworkBehaviour), "get_IsOwner")]
    public static bool IsOwner(NetworkBehaviour __instance)
    {
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool ExplodePrefix(ProximityMine __instance)
    {
        // PhysTPTrapData trap_data = __instance.GetComponent<PhysTPTrapData>();
        // KokiDebug.Log(trap_data.otherTrap);
        // if (!trap_data.otherTrap || !IsOwner(__instance))
        // { KokiDebug.Log("blocked explo"); return false; }

        // Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);
        // if (colliders.Length != 0)
        // {
        //     foreach (Collider c in colliders)
        //     {
        //         FirstPersonController fpc = c.GetComponent<FirstPersonController>();
        //         if (fpc)
        //         {
        //             KokiDebug.Log(trap_data.otherTrap.transform.position);
        //             trap_data.otherTrap.HandleExplosion();
        //             // fpc.Teleport(trap_data.otherTrap.transform.position, angle: 0, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: false);
        //             break;
        //         }
        //     }
        // }
        // __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "SpawnObject")]
    [HarmonyPrefix]
    static void SpawnObjectPrefix(WeaponHandSpawner __instance, ref GameObject obj)
    {
        NonPhysTPTrapData connecting_data = __instance.gameObject.GetComponent<NonPhysTPTrapData>();
        if (connecting_data)
        {
            KokiDebug.Log(obj);
            Transform baseVisualParent = obj.transform;
            baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
            GameObject meshInstance = UnityEngine.Object.Instantiate(PhysMineMesh);
            meshInstance.transform.SetParent(baseVisualParent);
            // PhysTPTrapData new_trap = obj.AddComponent<PhysTPTrapData>();

            // if (connecting_data.origTrap)
            // {
            //     connecting_data.origTrap.GetComponent<PhysTPTrapData>().otherTrap = obj;
            //     new_trap.otherTrap = connecting_data.origTrap;
            // }
            // else
            // {
            //     connecting_data.origTrap = obj;
            // }
        }
    }
}

public class NonPhysTPTrapData : MonoBehaviour
{
    public GameObject origTrap;
}

public class PhysTPTrapData : MonoBehaviour
{
    public GameObject otherTrap;
}