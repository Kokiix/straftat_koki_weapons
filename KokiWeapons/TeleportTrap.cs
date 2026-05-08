using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject NonPhysGO;

    public static GameObject BaseMineMesh;
    public static GameObject PhysMineMesh;

    public static GameObject GetNonPhysGO()
    {
        if (NonPhysGO) return NonPhysGO;

        BaseMineMesh = Object.Instantiate(BaseMineMesh);
        PhysMineMesh = Object.Instantiate(PhysMineMesh);

        NonPhysGO = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        NonPhysGO.SetActive(false);
        NonPhysGO.AddComponent<TrapPair>();

        ItemBehaviour ib = NonPhysGO.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport trap";

        WeaponHandSpawner spawner = NonPhysGO.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform baseVisualParent = NonPhysGO.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        BaseMineMesh.transform.SetParent(baseVisualParent);

        Transform physObjMine = spawner.objToSpawn.transform;
        physObjMine.Find("PF_APMine_00").gameObject.SetActive(false);
        PhysMineMesh.transform.SetParent(physObjMine);

        KokiDebug.Log(NotInHotReload(physObjMine.gameObject));
        if (NotInHotReload(physObjMine.gameObject))
            physObjMine.gameObject.AddComponent<TPTrapLink>();

        return NonPhysGO;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(NetworkBehaviour), "IsOwner", MethodType.Getter)]
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

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static bool MinePlacementPre(WeaponHandSpawner __instance, GameObject obj, Vector3 position, Quaternion rotation)
    {
        TrapPair connector = __instance.gameObject.GetComponent<TrapPair>();
        if (PauseManager.BetweenRounds || !connector) return true;

        GameObject physMine = UnityEngine.Object.Instantiate(obj, position, rotation);
        InstanceFinder.ServerManager.Spawn(physMine);
        physMine.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);
        physMine.GetComponent<ProximityMine>().sync___set_value_weapon((Weapon)__instance, true);

        TPTrapLink new_trap = physMine.GetComponent<TPTrapLink>();
        if (connector.origTrap)
        {
            connector.origTrap.GetComponent<TPTrapLink>().otherTrap = physMine;
            new_trap.otherTrap = connector.origTrap;
        }
        else
        {
            connector.origTrap = physMine;
        }
        return false;
    }

    public static bool NotInHotReload(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "TPTrapLink") return false;
        }
        return true;
    }
}

public class TrapPair : MonoBehaviour
{
    public GameObject origTrap;
}

public class TPTrapLink : MonoBehaviour
{
    public GameObject otherTrap;
}

