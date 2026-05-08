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
        PhysTPTrapData trap_data = __instance.GetComponent<PhysTPTrapData>();
        if (!trap_data || !IsOwner(__instance)) 
            return true;

        Collider[] array = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);
        if (array.Length != 0)
        {
            KokiDebug.Log("hit");
        }
        __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void PlaceObjectPrefix(WeaponHandSpawner __instance, ref GameObject obj)
    {
        NonPhysTPTrapData connecting_data = __instance.gameObject.GetComponent<NonPhysTPTrapData>();
        if (connecting_data)
        {
            KokiDebug.Log("placing teleport trap!");
            obj = UnityEngine.Object.Instantiate(GetPhysGO(originalGO: obj));
            PhysTPTrapData new_trap = obj.AddComponent<PhysTPTrapData>();

            if (connecting_data.origTrap)
            {
                connecting_data.origTrap.GetComponent<PhysTPTrapData>().destination = obj.transform.position;
                new_trap.destination = connecting_data.origTrap.transform.position;
            }
            else
            {
                connecting_data.origTrap = obj;
            }
        }
    }
}

public class NonPhysTPTrapData : MonoBehaviour
{
    public GameObject origTrap;
}

public class PhysTPTrapData : MonoBehaviour
{
    public Vector3 destination;
}